using System.Text.RegularExpressions;
using MQR.Services.Instructions.Models.Parsers;

namespace MQR.Services.Instructions;

/// <summary>
/// Extension methods for working with parse instruction sets.
/// </summary>
public static class ParseInstructionSetExtensions
{
    /// <summary>
    /// Checks if the given text matches the error identification pattern.
    /// </summary>
    public static bool IsMatch(this ErrorIdentification errorId, string text)
    {
        if (errorId?.RegExPattern?.Pattern == null)
        {
            return false;
        }

        var regex = new Regex(errorId.RegExPattern.Pattern, errorId.RegExPattern.RegExOptions);
        return regex.IsMatch(text);
    }

    /// <summary>
    /// Checks if the given text matches the end of data identification pattern.
    /// </summary>
    public static bool IsMatch(this EndOfDataIdentification endOfData, string text)
    {
        if (endOfData?.RegExPattern?.Pattern == null)
        {
            return false;
        }

        var regex = new Regex(endOfData.RegExPattern.Pattern, endOfData.RegExPattern.RegExOptions);
        return regex.IsMatch(text);
    }

    /// <summary>
    /// Checks if the given text matches the validation count identification pattern.
    /// </summary>
    public static bool IsMatch(this ValidationCountIdentificationMark validation, string text)
    {
        if (validation?.RegExPattern?.Pattern == null)
        {
            return false;
        }

        var regex = new Regex(validation.RegExPattern.Pattern, validation.RegExPattern.RegExOptions);
        return regex.IsMatch(text);
    }

    /// <summary>
    /// Extracts the validation count value from the text.
    /// </summary>
    public static int GetValue(this ValidationCountIdentificationMark validation, string text)
    {
        if (validation?.RegExPattern?.Pattern == null)
        {
            return -1;
        }

        var regex = new Regex(validation.RegExPattern.Pattern, validation.RegExPattern.RegExOptions);
        var match = regex.Match(text);

        if (match.Success && match.Groups.Count > 1)
        {
            return int.TryParse(match.Groups[1].Value, out var value) ? value : -1;
        }

        return -1;
    }

    /// <summary>
    /// Checks if the parse option identifier matches the given text.
    /// </summary>
    public static bool IsMatch(this ParseOptionIdentifier optionId, string text)
    {
        if (optionId?.RegExPattern?.Pattern == null)
        {
            return false;
        }

        var regex = new Regex(optionId.RegExPattern.Pattern, optionId.RegExPattern.RegExOptions);
        return regex.IsMatch(text);
    }

    /// <summary>
    /// Checks if the parse option matches the given text.
    /// </summary>
    public static bool IsMatch(this ParseOption option, string text)
    {
        return option?.ParseOptionIdentifiers?.Single().IsMatch(text) ?? false;
    }

    /// <summary>
    /// Checks if the row identification matches the given text.
    /// </summary>
    public static bool IsMatch(this RowIdentification rowId, string text)
    {
        if (rowId?.RegExPattern?.Pattern == null)
        {
            return false;
        }

        var regex = new Regex(rowId.RegExPattern.Pattern, rowId.RegExPattern.RegExOptions);
        return regex.IsMatch(text);
    }

    /// <summary>
    /// Checks if the parse category matches the given text.
    /// </summary>
    public static bool IsMatch(this ParseCategory category, string text)
    {
        return category?.RowIdentifications?.Single().IsMatch(text) ?? false;
    }

    /// <summary>
    /// Checks if the sub-row identification matches the given text.
    /// </summary>
    public static bool IsMatch(this SubRowIdentification subRowId, string text)
    {
        if (subRowId?.RegExPattern?.Pattern == null)
        {
            return false;
        }

        var regex = new Regex(subRowId.RegExPattern.Pattern, subRowId.RegExPattern.RegExOptions);
        return regex.IsMatch(text);
    }

    /// <summary>
    /// Builds a unique key for the row based on field definitions.
    /// </summary>
    public static string BuildKey(this ParseCategory category, string text, int rowNumber, int lineNumber)
    {
        if (category.KeyDefinitions.Count == 0)
        {
            return lineNumber.ToString();
        }

        var keyParts = new List<string>();
        foreach (var keyDef in category.KeyDefinitions)
        {
            var value = keyDef.GetValue(text);
            keyParts.Add(value);
        }

        return string.Join("_", keyParts);
    }

    /// <summary>
    /// Extracts the value from text using the field definition.
    /// </summary>
    public static string GetValue(this FieldDefinition fieldDef, string text)
    {
        if (fieldDef.RegExPattern?.Pattern != null)
        {
            var regex = new Regex(fieldDef.RegExPattern.Pattern, fieldDef.RegExPattern.RegExOptions);
            var match = regex.Match(text);

            if (match.Success)
            {
                // If there's a capture group, use the first one; otherwise use the whole match
                return match.Groups.Count > 1 ? match.Groups[1].Value.Trim() : match.Value.Trim();
            }

            return string.Empty;
        }

        // Use position-based extraction
        if (fieldDef is { XPos: >= 0, Length: > 0 } && text.Length >= fieldDef.XPos)
        {
            var endPos = Math.Min(fieldDef.XPos.Value + fieldDef.Length.Value, text.Length);
            return text.Substring(fieldDef.XPos.Value, endPos - fieldDef.XPos.Value).Trim();
        }

        return string.Empty;
    }

    /// <summary>
    /// Extracts the value from text using the key definition.
    /// </summary>
    public static string GetValue(this KeyDefinition keyDef, string text)
    {
        if (keyDef.RegExPattern?.Pattern != null)
        {
            var regex = new Regex(keyDef.RegExPattern.Pattern, keyDef.RegExPattern.RegExOptions);
            var match = regex.Match(text);

            if (match.Success)
            {
                return match.Groups.Count > 1 ? match.Groups[1].Value.Trim() : match.Value.Trim();
            }

            return string.Empty;
        }

        // Use position-based extraction
        if (keyDef is { XPos: >= 0, Length: > 0 } && text.Length >= keyDef.XPos)
        {
            var endPos = Math.Min(keyDef.XPos.Value + keyDef.Length.Value, text.Length);
            return text.Substring(keyDef.XPos.Value, endPos - keyDef.XPos.Value).Trim();
        }

        return string.Empty;
    }

    /// <summary>
    /// Checks if the instruction set indicates end of data for the given text.
    /// </summary>
    public static bool IsEndOfData(this ParseInstructionSet instructionSet, string text)
    {
        return instructionSet?.EndOfDataIdentification?.IsMatch(text) ?? false;
    }

    /// <summary>
    /// Gets the sub-row search line count for the category.
    /// </summary>
    public static int GetSubRowSearchLineCount(this ParseCategory category)
    {
        return category.SubRowSearchLineCount ?? 0;
    }

    /// <summary>
    /// Parses regex options from a string.
    /// </summary>
    private static RegexOptions ParseRegexOptions(string optionsString)
    {
        if (string.IsNullOrEmpty(optionsString) || optionsString == "None")
        {
            return RegexOptions.None;
        }

        var options = RegexOptions.None;
        var parts = optionsString.Split(',', '|');

        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (Enum.TryParse<RegexOptions>(trimmed, true, out var option))
            {
                options |= option;
            }
        }

        return options;
    }
}
