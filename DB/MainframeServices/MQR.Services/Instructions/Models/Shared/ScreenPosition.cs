namespace MQR.Services.Instructions.Models.Shared;

/// <summary>
/// Defines a rectangular area on the mainframe screen.
/// </summary>
public class ScreenPosition
{
    public uint? FieldNumber { get; set; }
    
    /// <summary>Starting column position.</summary>
    public uint? StartColumn { get; init; }

    /// <summary>Starting row position.</summary>
    public uint? StartRow { get; init; }
    
    public override string ToString() => FieldNumber.HasValue ? $"Field:{FieldNumber}" : $"({StartColumn},{StartRow})";

}