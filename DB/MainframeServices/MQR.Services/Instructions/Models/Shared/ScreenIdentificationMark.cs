namespace MQR.Services.Instructions.Models.Shared;

/// <summary>
/// Identifies a specific screen by pattern matching in a defined area.
/// </summary>
public class ScreenIdentificationMark
{
    /// <summary>Identifier for this screen mark.</summary>
    public required string Identifier { get; init; }

    /// <summary>Regular expression pattern to match.</summary>
    public required RegExPattern RegExPattern { get; init; }

    /// <summary>Screen area where the pattern should be found.</summary>
    public required ScreenArea ScreenArea { get; init; }
    
    /// <summary>Duration to wait for this mark to appear.</summary>
    public TimeSpan? WaitPeriod { get; init; }

    /// <summary>Whether to skip this mark if it is not found.</summary>   
    public bool? ExceptIfNotFound { get; set; }

    public override string ToString() => Identifier;
}