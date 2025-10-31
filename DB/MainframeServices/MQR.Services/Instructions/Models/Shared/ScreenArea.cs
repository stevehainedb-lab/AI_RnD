namespace MQR.Services.Instructions.Models.Shared;

/// <summary>
/// Defines a rectangular area on the mainframe screen.
/// </summary>
public class ScreenArea
{
    /// <summary>
    /// Short-circuit flag indicating to scan the entire screen.
    /// </summary>
    public bool Fullscreen { get; set; }

    /// <summary>
    /// flag indicating teh field to read.
    /// </summary>   
    public uint? Field { get; set; }

    /// <summary>Starting column position.</summary>
    public uint? StartColumn { get; init; }

    /// <summary>Starting row position.</summary>
    public uint? StartRow { get; init; }

    /// <summary>Ending column position.</summary>
    public uint? EndColumn { get; init; }

    /// <summary>Ending row position.</summary>
    public uint? EndRow { get; init; }
    public override string ToString()
    {
        if (Fullscreen) return "Fullscreen";
        
        if (Field.HasValue) return $"Field={Field}";
        
        var start = (StartRow.HasValue && StartColumn.HasValue)
            ? $"({StartRow},{StartColumn})"
            : "(?,?)";

        var end = (EndRow.HasValue && EndColumn.HasValue)
            ? $"({EndRow},{EndColumn})"
            : "(?,?)";

        return $"{start} → {end}";
    }
}