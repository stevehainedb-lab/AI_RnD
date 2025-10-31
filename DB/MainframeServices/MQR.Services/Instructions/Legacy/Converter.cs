using System.Text.Json;
using System.Text.Json.Serialization;
using MQR.Services.Instructions.Legacy.Logon;
using System.Xml.Serialization;
using MQR.Services.Instructions.Legacy.Parsing;
using MQR.Services.Instructions.Legacy.Query;

namespace MQR.Services.Instructions.Legacy.Testing;

/// <summary>
/// Utility class for deserializing and converting legacy instruction sets.
/// </summary>
public class Converter
{
    /// <summary>
    /// Deserializes all XML files of type T from the specified folder.
    /// </summary>
    public void ConvertAllToJson(string inputFolder, string outputFolder)
    {
        if (!Directory.Exists(inputFolder))
        {
            Console.WriteLine($"Folder not found: {inputFolder}");
            return;
        }

        var serializer = new XmlSerializer(typeof(QueryInstructionSet));
        var results = new List<QueryInstructionSet>();

        foreach (string file in Directory.GetFiles(inputFolder, "*.xml", SearchOption.TopDirectoryOnly))
        {
            try
            {
                using var stream = File.OpenRead(file);
                var obj = (QueryInstructionSet?)serializer.Deserialize(stream);
                if (obj != null)
                {
                    results.Add(obj);
                    Console.WriteLine($"✅ Loaded: {Path.GetFileName(file)}");
                }
                else
                {
                    Console.WriteLine($"⚠️  Deserialized null object: {Path.GetFileName(file)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error reading {Path.GetFileName(file)}: {ex.Message}");
            }
        }

        var mapped = results.Select(ConvertLegacyQuerySetToNew).ToList();
        
        foreach (var logonInstructionSet in mapped)
        {
            var json = JsonSerializer.Serialize(logonInstructionSet, new JsonSerializerOptions()
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() }
            });
            
            var path = Path.Combine(outputFolder, $"{logonInstructionSet.Identifier}.json");
            File.WriteAllText(path, json);
        }
    }

    /// <summary>
    /// Converts a legacy logon instruction set to the modern model.
    /// </summary>
    public LogonInstructionSet ConvertLegacyLogonSetToNew(LegacyLogonInstructionSet legacy)
    {
        return LogonInstructionSetMapper.MapToNew(legacy);
    }

    /// <summary>
    /// Converts a legacy query instruction set to the modern model.
    /// </summary>
    public Models.Queries.QueryInstructionSet ConvertLegacyQuerySetToNew(
        QueryInstructionSet legacy)
    {
        return QueryInstructionSetMapper.MapToNew(legacy);
    }

    /// <summary>
    /// Converts a legacy parse instruction set to the modern model.
    /// </summary>
    public Models.Parsers.ParseInstructionSet ConvertLegacyParseSetToNew(
        ParseInstructionSet legacy)
    {
        return ParseInstructionSetMapper.MapToNew(legacy);
    }
}