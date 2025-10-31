using System.Text.Json;
using System.Text.Json.Nodes;
using System.IO;

namespace MQR.Services.Instructions; 
public static class JsonInstructionComposer
{
    /// <summary>
    /// Compose a final object of type T from a header JSON:
    /// {
    ///   "BaseFile": "./Base.json",
    ///   "Templates": [
    ///     { "#/Path/To/Object": "./Partial.json#/SubObj" },
    ///     { "#/Array/-": "./Item.json" },
    ///     { "#/Scalar": 10 },
    ///     { "TopLevelKey": "./Other.json" }
    ///   ]
    /// }
    /// </summary>
    public static async Task<T> LoadFromHeaderAsync<T>(
        string headerPath,
        JsonSerializerOptions? options = null,
        CancellationToken ct = default)
    {
        try
        {
            var fullHeader = Path.GetFullPath(headerPath);
            var baseDir = Path.GetDirectoryName(fullHeader)!;

            await using var hfs = File.OpenRead(fullHeader);
            return await LoadFromHeaderAsync<T>(hfs, baseDir, options, ct);
        }
        catch (Exception e) when (e is not InvalidDataException)
        {
            throw new InvalidOperationException($"Error loading header JSON from file {headerPath}", e);
        }
    }

    /// <summary>
    /// Compose a final object of type T from a header JSON provided as a stream, using
    /// the specified base directory to resolve relative file references.
    /// </summary>
    public static async Task<T> LoadFromHeaderAsync<T>(
        Stream headerStream,
        string baseDirectory,
        JsonSerializerOptions? options = null,
        CancellationToken ct = default)
    {
        var baseDir = Path.GetFullPath(baseDirectory);

        // Load header
        var header = await ParseNodeAsync(headerStream, ct) as JsonObject
            ?? throw new InvalidOperationException("Header JSON must be an object.");

        if (!header.TryGetPropertyValue("BaseFile", out var baseFileNode)
            || baseFileNode is not JsonValue bv
            || !bv.TryGetValue<string>(out var baseFile))
        {
            throw new InvalidOperationException("Header must contain string property 'BaseFile'.");
        }

        var basePath = Path.GetFullPath(Path.Combine(baseDir, baseFile));
        if (!File.Exists(basePath))
            throw new FileNotFoundException("Base file not found.", basePath);

        // Load base document
        await using var bfs = File.OpenRead(basePath);
        var baseDoc = await ParseNodeAsync(bfs, ct)
                     ?? throw new InvalidOperationException("Base JSON empty or invalid.");

        // Apply templates
        if (header.TryGetPropertyValue("Templates", out var templatesNode) && templatesNode is JsonArray arr)
        {
            foreach (var mapping in arr)
            {
                if (mapping is not JsonObject mapObj || mapObj.Count != 1)
                    throw new InvalidOperationException("Each Templates entry must be an object with exactly one property.");

                var (destKey, valNode, isRef) = ToKeyAndValue(mapObj);

                JsonNode included = isRef
                    ? await ResolveRefAsync(valNode!.GetValue<string>(), baseDir, ct)
                    : await ResolveLiteralAsync(valNode!, baseDir, ct);

                if (LooksLikePointer(destKey))
                {
                    SetByPointer(ref baseDoc, destKey, included);
                }
                else
                {
                    if (baseDoc is not JsonObject rootObj)
                        throw new InvalidOperationException("Base JSON root must be an object to set a top-level key.");

                    if (rootObj.TryGetPropertyValue(destKey, out var existing) &&
                        existing is JsonObject eo &&
                        included is JsonObject io)
                    {
                        rootObj[destKey] = DeepMerge(eo, io);
                    }
                    else
                    {
                        rootObj[destKey] = included.DeepClone();
                    }
                }
            }
        }

        var json = baseDoc.ToJsonString(options);
        try
        {
            return JsonSerializer.Deserialize<T>(json, options ?? new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            })!;

        }
        catch (Exception e)
        {
            throw new InvalidDataException($"Error deserializing JSON from base file {basePath}", e)
            {
                Data = { {"json", json }}
            };
        }
    }

    // ---------------- Template value handling ----------------

    private static (string Key, JsonNode Value, bool IsRef) ToKeyAndValue(JsonObject singleProp)
    {
        var kv = singleProp.First();
        var key = kv.Key;
        var val = kv.Value ?? throw new InvalidOperationException("Template mapping must have a non-null value.");

        // Treat strings that look like file paths (ending .json or containing path separators) as refs.
        if (val is JsonValue jv && jv.TryGetValue<string>(out var s) && LooksLikeRefPath(s))
            return (key, JsonValue.Create(s)!, true);

        // Otherwise literal JSON (number, bool, null, object, array, or plain string)
        return (key, val, false);
    }

    private static bool LooksLikeRefPath(string s)
    {
        var hashIdx = s.IndexOf('#');
        var path = hashIdx >= 0 ? s[..hashIdx] : s;
        return !string.IsNullOrWhiteSpace(path)
               && (path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                   || path.Contains(Path.DirectorySeparatorChar)
                   || path.Contains('/'));
    }

    private static bool LooksLikePointer(string key) =>
        key.StartsWith("#/") || key.StartsWith("/");

    // ---------------- Ref / Literal resolution ----------------

    private static async Task<JsonNode> ResolveRefAsync(string refValue, string baseDir, CancellationToken ct)
    {
        // Split path and optional fragment
        string pathPart, fragment;
        var idx = refValue.IndexOf('#');
        if (idx >= 0) { pathPart = refValue[..idx]; fragment = refValue[(idx + 1)..]; }
        else { pathPart = refValue; fragment = string.Empty; }

        if (string.IsNullOrWhiteSpace(pathPart))
            throw new InvalidOperationException("Reference path cannot be empty (must point to a file).");

        var fullPath = Path.GetFullPath(Path.Combine(baseDir, pathPart));
        if (!IsPathUnderBase(fullPath, baseDir))
            throw new InvalidOperationException($"Reference escapes base directory: {refValue}");
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Referenced JSON not found: {refValue}", fullPath);

        JsonNode doc;
        await using (var fs = File.OpenRead(fullPath))
        {
            doc = await ParseNodeAsync(fs, ct)
                ?? throw new InvalidOperationException($"Referenced JSON empty or invalid: {refValue}");
        }

        if (string.IsNullOrEmpty(fragment))
            return doc.DeepClone();

        var node = ApplyJsonPointer(doc, fragment);
        return node.DeepClone();
    }

    private static Task<JsonNode> ResolveLiteralAsync(JsonNode node, string baseDir, CancellationToken ct)
        => Task.FromResult(node.DeepClone());

    // ---------------- Pointer set/merge ----------------

    private static void SetByPointer(ref JsonNode root, string pointer, JsonNode value)
    {
        var p = pointer.StartsWith("/") ? pointer
              : pointer.StartsWith("#/") ? pointer[1..]
              : "/" + pointer;

        var tokens = p.Split('/').Skip(1).Select(DecodePointerToken).ToArray();

        if (tokens.Length == 0)
        {
            root = value.DeepClone();
            return;
        }

        JsonNode current = root;
        for (int i = 0; i < tokens.Length - 1; i++)
        {
            var t = tokens[i];

            if (current is JsonObject o)
            {
                if (!o.TryGetPropertyValue(t, out var child) || child is null)
                {
                    // auto-create an object along the path
                    var created = new JsonObject();
                    o[t] = created;
                    current = created;
                }
                else current = child;
            }
            else if (current is JsonArray a)
            {
                // navigating inside an array requires numeric index
                if (!int.TryParse(t, out var index))
                    throw new InvalidOperationException($"Pointer segment '{t}' not a valid array index.");
                while (a.Count <= index) a.Add(new JsonObject());
                current = a[index]!;
            }
            else
            {
                throw new InvalidOperationException($"Cannot traverse into primitive at '{t}'.");
            }
        }

        var last = tokens[^1];

        // Set under object
        if (current is JsonObject obj)
        {
            if (obj.TryGetPropertyValue(last, out var existing) &&
                existing is JsonObject eo &&
                value is JsonObject io)
            {
                obj[last] = DeepMerge(eo, io);
            }
            else
            {
                // If existing is an array and last == "-" (rare for object), treat as replace
                obj[last] = value.DeepClone();
            }
            return;
        }

        // Set under array
        if (current is JsonArray arr)
        {
            if (last == "-")
            {
                // Append
                arr.Add(value.DeepClone());
                return;
            }

            if (!int.TryParse(last, out var index))
                throw new InvalidOperationException($"Pointer segment '{last}' not a valid array index.");
            while (arr.Count <= index) arr.Add(null);
            arr[index] = value.DeepClone();
            return;
        }

        throw new InvalidOperationException($"Cannot set value under primitive at '{last}'.");
    }

    // ---------------- Merge & pointer utilities ----------------

    private static JsonNode DeepMerge(JsonNode target, JsonNode overrides)
    {
        if (target is JsonObject to && overrides is JsonObject oo)
        {
            foreach (var kv in oo)
            {
                if (kv.Value is null)
                {
                    to[kv.Key] = null;
                    continue;
                }

                if (to.TryGetPropertyValue(kv.Key, out var existing) && existing is not null)
                    to[kv.Key] = DeepMerge(existing, kv.Value);
                else
                    to[kv.Key] = kv.Value.DeepClone();
            }
            return to;
        }

        if (target is JsonArray && overrides is JsonArray)
        {
            // Replace arrays by default (use pointer "-” to append instead).
            return overrides.DeepClone();
        }

        // Primitive or differing types → overrides win
        return overrides.DeepClone();
    }

    private static JsonNode ApplyJsonPointer(JsonNode document, string pointer)
    {
        var p = pointer.StartsWith("/") ? pointer : "/" + pointer;
        var tokens = p.Split('/').Skip(1).Select(DecodePointerToken).ToArray();

        JsonNode current = document;
        foreach (var token in tokens)
        {
            if (current is JsonObject obj)
            {
                if (!obj.TryGetPropertyValue(token, out var child) || child is null)
                    throw new KeyNotFoundException($"JSON Pointer segment '{token}' not found.");
                current = child;
            }
            else if (current is JsonArray arr)
            {
                if (!int.TryParse(token, out var i) || i < 0 || i >= arr.Count)
                    throw new IndexOutOfRangeException($"Array index '{token}' out of bounds.");
                current = arr[i]!;
            }
            else
            {
                throw new InvalidOperationException($"Cannot traverse primitive at '{token}'.");
            }
        }
        return current;
    }

    private static string DecodePointerToken(string token) =>
        Uri.UnescapeDataString(token).Replace("~1", "/").Replace("~0", "~");

    private static bool IsPathUnderBase(string fullPath, string baseDir)
    {
        var baseNorm = EnsureTrailingSeparator(Path.GetFullPath(baseDir));
        var fullNorm = Path.GetFullPath(fullPath);
        return fullNorm.StartsWith(baseNorm, StringComparison.OrdinalIgnoreCase);
    }

    private static string EnsureTrailingSeparator(string path)
    {
        if (string.IsNullOrEmpty(path)) return path;
        char sep = Path.DirectorySeparatorChar;
        return path.EndsWith(sep) ? path : path + sep;
    }

    private static async Task<JsonNode?> ParseNodeAsync(Stream stream, CancellationToken ct)
    {
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        return JsonNode.Parse(doc.RootElement.GetRawText());
    }
}
