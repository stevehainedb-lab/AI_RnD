using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using MQR.Services.Instructions;
using MQR.Services.Instructions.Models.Parsers;
using MQR.WebAPI.ServiceModel;

namespace MQR.Services.Queues;

/// <summary>
/// Parses raw mainframe output, according to a given parse instruction set.
/// </summary>
public sealed partial class InstructionBasedParser(ILogger<InstructionBasedParser> logger)
{
    private static readonly Regex LineSplitRegex = CreateLineSplitRegex();
    
    [GeneratedRegex(@"\r\n|\r|\n|$|\f")]
    private static partial Regex CreateLineSplitRegex();

    public Task<QueryResultSection[]> ParseByInstructionSet(ParseNotification notification,
        ParseInstructionSet instructions,
        string parseableData)
    {
        var printResponses = Regex
            .Split(parseableData, notification.ParseSplitRegex ?? @"\f")
            .Where(s => !string.IsNullOrWhiteSpace(s));

        var allSections = new Dictionary<string, QueryResultSection>();

        foreach (var printResponse in printResponses)
        {
            var sessionIdentifier = notification.RequestSessionIdentifier ?? "Unknown";

            var sections = ParsePrintResponse(
                instructions,
                sessionIdentifier,
                printResponse);

            // Merge sections from this print response
            foreach (var section in sections)
            {
                if (allSections.TryGetValue(section.Identifier, out var existingSection))
                {
                    // Merge rows into existing section
                    var mergedRows = existingSection.Rows.Concat(section.Rows).ToArray();
                    existingSection.Rows = mergedRows;
                }
                else
                {
                    allSections[section.Identifier] = section;
                }
            }
        }

        return Task.FromResult(allSections.Values.ToArray());
    }

    /// <summary>
    /// Parses a print response using the instruction set.
    /// Returns the parsed sections.
    /// </summary>
    private QueryResultSection[] ParsePrintResponse(
        ParseInstructionSet parseInstructions,
        string requestSessionIdentifier,
        string printResponse)
    {
        var lines = LineSplitRegex.Split(printResponse);

        logger.LogDebug(
            "Parsing {LineCount} rows via {InstructionSet}",
            lines.Length,
            parseInstructions.Identifier);

        // Find the correct parse option to use
        var parseOption = FindParseOption(parseInstructions, printResponse);

        // Parse state
        ParseCategory? currentCategory = null;
        QueryResultSection? currentSection = null;

        var validationMark = parseInstructions.ValidationCountIdentificationMarks.Single();
        var pattern = validationMark.RegExPattern.Pattern;
        var validateRows = pattern != null;
        var validationCount = -1;
        var itemCount = 0;

        var sections = new Dictionary<string, QueryResultSection>();

        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var line = lines[lineIndex];

            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            // Check if we need to switch categories
            if (currentCategory is null || !currentCategory.IsMatch(line))
            {
                currentCategory = FindMatchingCategory(line, parseOption.ParseCategories);

                if (currentCategory != null)
                {
                    // Get or create section for this category
                    if (sections.TryGetValue(currentCategory.Identifier, out currentSection))
                    {
                        // Section already exists
                    }
                    else
                    {
                        currentSection = new QueryResultSection
                        {
                            Identifier = currentCategory.Identifier,
                            Rows = []
                        };
                        sections[currentCategory.Identifier] = currentSection;
                    }
                }
            }

            if (currentCategory is null)
            {
                // Check for validation count
                if (validateRows && validationMark!.IsMatch(line))
                {
                    validationCount = validationMark.GetValue(line);
                }

                // Check for end of data
                if (parseInstructions.IsEndOfData(line))
                {
                    break;
                }
            }
            else
            {
                // Build the row
                var rowKey = currentCategory.BuildKey(line, currentSection!.Rows.Length + 1, lineIndex + 1);
                var rowExists = currentSection.Rows.Any(r => r.Identifier == rowKey);

                if (!rowExists)
                {
                    // Collect lines for sub-row searching
                    var searchLines = new string[currentCategory.GetSubRowSearchLineCount() + 1];
                    searchLines[0] = line;

                    for (var i = 1; i <= currentCategory.GetSubRowSearchLineCount(); i++)
                    {
                        searchLines[i] = (lineIndex + i < lines.Length) ? lines[lineIndex + i] : string.Empty;
                    }

                    // Extract fields
                    var fields = ExtractFields(currentCategory, searchLines);

                    var row = new QueryResultRow
                    {
                        Identifier = rowKey,
                        FullLine = parseInstructions.CaptureRaw ? line : null,
                        Fields = fields.ToArray()
                    };

                    var rowList = currentSection.Rows.ToList();
                    rowList.Add(row);
                    currentSection.Rows = rowList.ToArray();

                    itemCount++;
                }
                else if (parseInstructions.IncludeDuplicatesForValidation)
                {
                    itemCount++;
                }
            }
        }

        // Validate row count if required
        if (validateRows)
        {
            if (validationCount == -1)
            {
                throw new InvalidOperationException("Could not find count row for validation");
            }

            if (itemCount != validationCount)
            {
                throw new InvalidOperationException(
                    $"Validation count does not match. Found {itemCount} items, validation is {validationCount}. " +
                    $"From {requestSessionIdentifier} using {parseInstructions.Identifier} parse instruction set!");
            }
        }

        // Return all sections
        return sections.Values.ToArray();
    }

    /// <summary>
    /// Finds the appropriate parse option to use based on the print response.
    /// </summary>
    private ParseOption FindParseOption(ParseInstructionSet instructionSet, string printResponse)
    {
        ParseOption? defaultOption = null;
        ParseOption? matchedOption = null;

        foreach (var option in instructionSet.ParseOptions)
        {
            // Check if this option has an identifier (non-default)
            if (option.ParseOptionIdentifiers != null)
            {
                if (option.IsMatch(printResponse))
                {
                    if (matchedOption != null)
                    {
                        throw new InvalidOperationException(
                            $"{instructionSet.Identifier} instruction set has more than one ParseOption that matches");
                    }

                    matchedOption = option;
                }
            }
            else
            {
                // This is a default option (no identifier)
                if (defaultOption != null)
                {
                    throw new InvalidOperationException(
                        $"{instructionSet.Identifier} instruction set has more than one Option with no OptionIdentificationMarks");
                }

                defaultOption = option;
            }
        }

        var selectedOption = matchedOption ?? defaultOption;

        if (selectedOption is null)
        {
            throw new InvalidOperationException(
                $"{instructionSet.Identifier} instruction set does not have a matching ParseOption");
        }

        return selectedOption;
    }

    /// <summary>
    /// Finds the category that matches the given line.
    /// </summary>
    private ParseCategory? FindMatchingCategory(string line, List<ParseCategory> categories)
    {
        return categories.FirstOrDefault(c => c.IsMatch(line));
    }

    /// <summary>
    /// Extracts fields from the given lines using the category's field definitions.
    /// </summary>
    private List<QueryResultField> ExtractFields(ParseCategory category, string[] lines)
    {
        var fields = new List<QueryResultField>();

        // Extract fields from main line
        foreach (var fieldDef in category.FieldDefinitions)
        {
            fields.Add(new QueryResultField
            {
                Identifier = fieldDef.Identifier,
                Value = fieldDef.GetValue(lines[0])
            });
        }

        // Extract fields from sub-rows
        foreach (var subRowId in category.SubRowIdentifications)
        {
            for (var i = 1; i < lines.Length; i++)
            {
                if (subRowId.IsMatch(lines[i]))
                {
                    fields.Add(new QueryResultField
                    {
                        Identifier = subRowId.FieldDefinition.Identifier,
                        Value = subRowId.FieldDefinition.GetValue(lines[i])
                    });
                }
            }
        }

        return fields;
    }
}