namespace MQR.Services.Instructions.Models.Parsers;

/// <summary>
/// Defines how to parse mainframe screen output into structured data.
/// </summary>
public class ParseInstructionSet
{
    /// <summary>Unique identifier for this parse instruction set.</summary>
    public required string Identifier { get; init; }

    /// <summary>Whether to capture the raw screen data along with parsed data.</summary>
    public bool CaptureRaw { get; init; }

    /// <summary>Whether to include duplicate rows when validating result counts.</summary>
    public bool IncludeDuplicatesForValidation { get; init; }

    /// <summary>Identifies when the end of data has been reached.</summary>
    public EndOfDataIdentification? EndOfDataIdentification { get; init; }

    /// <summary>Error patterns that indicate parsing should fail.</summary>
    public List<ErrorIdentification> ErrorIdentifications { get; init; } = [];

    /// <summary>Different parsing options that can be applied based on screen content.</summary>
    public List<ParseOption> ParseOptions { get; init; } = [];

    /// <summary>Marks used to validate the count of parsed items.</summary>
    public List<ValidationCountIdentificationMark> ValidationCountIdentificationMarks { get; init; } = [];

    public override string ToString() => Identifier;
}

/// <summary>
/// Identifies error conditions on the screen that should stop parsing.
/// </summary>
public class ErrorIdentification
{
    /// <summary>Identifier for this error condition.</summary>
    public required string Identifier { get; init; }

    /// <summary>Whether encountering this error should make the credential pool unavailable.</summary>
    public bool MakePoolUnavailable { get; init; }

    /// <summary>How long the pool should remain unavailable after this error.</summary>
    public TimeSpan? PoolUnavailablePeriod { get; init; }

    /// <summary>Regular expression pattern that identifies this error.</summary>
    public required Shared.RegExPattern RegExPattern { get; init; }

    /// <summary>Screen area where to look for this error pattern.</summary>
    public required Shared.ScreenArea ScreenArea { get; init; }

    public override string ToString() => Identifier;
}

/// <summary>
/// A parsing option that can be selected based on screen content.
/// Different options allow for different data layouts on the same query.
/// </summary>
public class ParseOption
{
    /// <summary>Identifier for this parse option.</summary>
    public required string Identifier { get; init; }

    /// <summary>Categories of data to parse under this option.</summary>
    public List<ParseCategory> ParseCategories { get; init; } = [];

    /// <summary>Identifiers that determine when to use this parse option.</summary>
    public List<ParseOptionIdentifier> ParseOptionIdentifiers { get; init; } = [];

    public override string ToString() => Identifier;
}

/// <summary>
/// A category of data to parse from the screen (e.g., a table of results).
/// </summary>
public class ParseCategory
{
    /// <summary>Identifier for this category.</summary>
    public required string Identifier { get; init; }

    /// <summary>Number of lines to search for sub-row patterns below the main row.</summary>
    public int? SubRowSearchLineCount { get; init; }

    /// <summary>Field definitions describing what data to extract from each row.</summary>
    public List<FieldDefinition> FieldDefinitions { get; init; } = [];

    /// <summary>Key definitions that uniquely identify each row.</summary>
    public List<KeyDefinition> KeyDefinitions { get; init; } = [];

    /// <summary>Patterns that identify the start of a data row.</summary>
    public List<RowIdentification> RowIdentifications { get; init; } = [];

    /// <summary>Patterns that identify sub-rows within a main row (e.g., detail lines).</summary>
    public List<SubRowIdentification> SubRowIdentifications { get; init; } = [];

    public override string ToString() => Identifier;
}

/// <summary>
/// Identifies the start of a data row in a category.
/// </summary>
public class RowIdentification
{
    /// <summary>Identifier for this row pattern.</summary>
    public required string Identifier { get; init; }

    /// <summary>Length of the text to match (null for variable length).</summary>
    public int? Length { get; init; }

    /// <summary>X position (column) where to start matching.</summary>
    public int? XPos { get; init; }

    /// <summary>Regular expression pattern that identifies this row.</summary>
    public required Shared.RegExPattern RegExPattern { get; init; }

    public override string ToString() => Identifier;
}

/// <summary>
/// Defines a field of data to extract from a row.
/// </summary>
public class FieldDefinition
{
    /// <summary>Identifier/name for this field (becomes the field name in the result).</summary>
    public required string Identifier { get; init; }

    /// <summary>Length of the field to extract (null for variable length).</summary>
    public int? Length { get; init; }

    /// <summary>X position (column) where this field starts.</summary>
    public int? XPos { get; init; }

    /// <summary>Regular expression pattern to extract this field's value.</summary>
    public Shared.RegExPattern? RegExPattern { get; init; }

    public override string ToString() => Identifier;
}

/// <summary>
/// Identifies when the end of data has been reached on the screen.
/// </summary>
public class EndOfDataIdentification
{
    /// <summary>Whether to throw an exception if this end-of-data marker is not found.</summary>
    public bool ExceptIfNotFound { get; init; }

    /// <summary>Identifier for this end-of-data marker.</summary>
    public required string Identifier { get; init; }

    /// <summary>Regular expression pattern that marks the end of data.</summary>
    public required Shared.RegExPattern RegExPattern { get; init; }

    /// <summary>Screen area where to look for the end-of-data marker.</summary>
    public required Shared.ScreenArea ScreenArea { get; init; }

    public override string ToString() => Identifier;
}

/// <summary>
/// Defines a key field that uniquely identifies a row (used for deduplication).
/// </summary>
public class KeyDefinition
{
    /// <summary>Identifier for this key field.</summary>
    public required string Identifier { get; init; }

    /// <summary>Length of the key field (-1 for variable length).</summary>
    public int? Length { get; init; }

    /// <summary>X position (column) where this key field starts.</summary>
    public int? XPos { get; init; }

    /// <summary>Regular expression pattern to extract this key's value.</summary>
    public Shared.RegExPattern? RegExPattern { get; init; }

    public override string ToString() => Identifier;
}

/// <summary>
/// A mark on the screen used to validate the count of parsed items.
/// </summary>
public class ValidationCountIdentificationMark
{
    /// <summary>Identifier for this validation mark.</summary>
    public required string Identifier { get; init; }

    /// <summary>Length of the count text to extract.</summary>
    public int? Length { get; init; }

    /// <summary>X position (column) where the count appears.</summary>
    public int? XPos { get; init; }

    /// <summary>Regular expression pattern to extract the count value.</summary>
    public required Shared.RegExPattern RegExPattern { get; init; }

    public override string ToString() => Identifier;
}

/// <summary>
/// Identifies when to use a specific parse option based on screen content.
/// </summary>
public class ParseOptionIdentifier
{
    /// <summary>Identifier for this option identifier.</summary>
    public required string Identifier { get; init; }

    /// <summary>Regular expression pattern that triggers this parse option.</summary>
    public required Shared.RegExPattern RegExPattern { get; init; }

    /// <summary>Screen area where to look for the identifier pattern.</summary>
    public required Shared.ScreenArea ScreenArea { get; init; }

    public override string ToString() => Identifier;
}

/// <summary>
/// Identifies a sub-row within a main data row (e.g., additional detail line).
/// </summary>
public class SubRowIdentification
{
    /// <summary>Identifier for this sub-row type.</summary>
    public required string Identifier { get; init; }

    /// <summary>Length of the text to match for this sub-row (-1 for variable length).</summary>
    public int? Length { get; init; }

    /// <summary>X position (column) where to start matching this sub-row.</summary>
    public int? XPos { get; init; }

    /// <summary>Field definition describing what data to extract from this sub-row.</summary>
    public required FieldDefinition FieldDefinition { get; init; }

    /// <summary>Regular expression pattern that identifies this sub-row.</summary>
    public required Shared.RegExPattern RegExPattern { get; init; }

    public override string ToString() => Identifier;
}