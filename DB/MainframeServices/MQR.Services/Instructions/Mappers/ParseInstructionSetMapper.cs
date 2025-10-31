using MQR.Services.Instructions.Models.Parsers;
using static MQR.Services.Instructions.Legacy.ParsingUtilities;

namespace MQR.Services.Instructions.Legacy;

/// <summary>
/// Maps legacy parse instruction sets to the modern model.
/// </summary>
public static class ParseInstructionSetMapper
{
    public static ParseInstructionSet MapToNew(Parsing.ParseInstructionSet legacy)
    {
        return new ParseInstructionSet
        {
            CaptureRaw = ParseBool(legacy.CaptureRaw),
            Identifier = legacy.Identifier ?? throw new InvalidOperationException("Identifier is required"),
            IncludeDuplicatesForValidation = ParseBool(legacy.IncludeDuplicatesForValidation),
            EndOfDataIdentification = legacy.EndOfDataIdentification != null
                ? MapEndOfDataIdentification(legacy.EndOfDataIdentification)
                : null,
            ErrorIdentifications = legacy.ErrorIdentification.Select(MapErrorIdentification).ToList(),
            ParseOptions = legacy.ParseOption.Select(MapParseOption).ToList(),
            ValidationCountIdentificationMarks = legacy.ValidationCountIdentificationMark
                .Select(MapValidationCountIdentificationMark)
                .ToList()
        };
    }

    private static ErrorIdentification MapErrorIdentification(
        Parsing.ErrorIdentification legacy)
    {
        return new ErrorIdentification
        {
            Identifier = NullIfEmpty(legacy.Identifier),
            MakePoolUnavailable = ParseBool(legacy.MakePoolUnavailable),
            PoolUnavailablePeriod = ParseTimeSpan(legacy.PoolUnavailablePeriod),
            RegExPattern = MapRegExPattern(legacy.RegExPattern),
            ScreenArea = MapScreenArea(legacy.ScreenArea)
        };
    }

    private static Models.Shared.ScreenArea MapScreenArea(Parsing.ScreenArea legacy)
    {
        return new Models.Shared.ScreenArea
        {
            EndColumn = ParseUnsignedInt(legacy.EndCol),
            EndRow = ParseUnsignedInt(legacy.EndRow),
            StartColumn = ParseUnsignedInt(legacy.StartCol) ?? 0,
            StartRow = ParseUnsignedInt(legacy.StartRow) ?? 0
        };
    }

    private static Models.Shared.RegExPattern MapRegExPattern(Parsing.RegExPattern legacy)
    {
        return new Models.Shared.RegExPattern
        {
            RegExOptions = ParseRegexOptions(legacy.RegexOptions),
            Pattern = legacy.Value ?? string.Empty
        };
    }

    private static ParseOption MapParseOption(Parsing.ParseOption legacy)
    {
        return new ParseOption
        {
            Identifier = legacy.Identifier ?? "[unknown]",
            ParseCategories = legacy.ParseCategory.Select(MapParseCategory).ToList(),
            ParseOptionIdentifiers = legacy.ParseOptionIdentifer.Select(MapParseOptionIdentifier).ToList()
        };
    }

    private static ParseCategory MapParseCategory(Parsing.ParseCategory legacy)
    {
        return new ParseCategory
        {
            Identifier = legacy.Identifier ?? "[unknown category]",
            SubRowSearchLineCount = ParseInt(legacy.SubRowSearchLineCount),
            FieldDefinitions = legacy.FieldDefintion.Select(MapFieldDefinition).ToList(),
            KeyDefinitions = legacy.KeyDefintion.Select(MapKeyDefinition).ToList(),
            RowIdentifications = legacy.RowIdentification.Select(MapRowIdentification).ToList(),
            SubRowIdentifications = legacy.SubRowIdentification.Select(MapSubRowIdentification).ToList()
        };
    }

    private static RowIdentification MapRowIdentification(Parsing.RowIdentification legacy)
    {
        return new RowIdentification
        {
            Identifier = NullIfEmpty(legacy.Identifier),
            Length = ParseInt(legacy.Length),
            XPos = ParseInt(legacy.XPos),
            RegExPattern = MapRegExPattern(legacy.RegExPattern)
        };
    }

    private static FieldDefinition MapFieldDefinition(Parsing.FieldDefintion legacy)
    {
        return new FieldDefinition
        {
            Identifier = legacy.Identifier,
            Length = ParseInt(legacy.Length),
            XPos = ParseInt(legacy.XPos),
            RegExPattern = MapRegExPattern(legacy.RegExPattern)
        };
    }

    private static EndOfDataIdentification MapEndOfDataIdentification(
        Parsing.EndOfDataIdentification legacy)
    {
        return new EndOfDataIdentification
        {
            ExceptIfNotFound = ParseBool(legacy.ExceptIfNotFound),
            Identifier = NullIfEmpty(legacy.Identifier),
            RegExPattern = MapRegExPattern(legacy.RegExPattern),
            ScreenArea = MapScreenArea(legacy.ScreenArea)
        };
    }

    private static KeyDefinition MapKeyDefinition(Parsing.KeyDefintion legacy)
    {
        return new KeyDefinition
        {
            Identifier = legacy.Identifier,
            Length = ParseInt(legacy.Length),
            XPos = ParseInt(legacy.XPos),
            RegExPattern = MapRegExPattern(legacy.RegExPattern)
        };
    }

    private static ValidationCountIdentificationMark MapValidationCountIdentificationMark(
        Parsing.ValidationCountIdentificationMark legacy)
    {
        return new ValidationCountIdentificationMark
        {
            Identifier = NullIfEmpty(legacy.Identifier),
            Length = ParseInt(legacy.Length),
            XPos = ParseInt(legacy.XPos),
            RegExPattern = MapRegExPattern(legacy.RegExPattern)
        };
    }

    private static ParseOptionIdentifier MapParseOptionIdentifier(
        Parsing.ParseOptionIdentifer legacy)
    {
        return new ParseOptionIdentifier
        {
            Identifier = NullIfEmpty(legacy.Identifier),
            RegExPattern = MapRegExPattern(legacy.RegExPattern),
            ScreenArea = MapScreenArea(legacy.ScreenArea)
        };
    }

    private static SubRowIdentification MapSubRowIdentification(
        Parsing.SubRowIdentification legacy)
    {
        return new SubRowIdentification
        {
            Identifier = legacy.Identifier ?? "[unknown sub-row identifier]",
            Length = ParseInt(legacy.Length),
            XPos = ParseInt(legacy.XPos),
            FieldDefinition = MapFieldDefinition(legacy.FieldDefintion),
            RegExPattern = MapRegExPattern(legacy.RegExPattern)
        };
    }
}