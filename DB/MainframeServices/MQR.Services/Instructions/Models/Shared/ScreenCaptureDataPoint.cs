namespace MQR.Services.Instructions.Models.Shared;

public class ScreenCaptureDataPoint
{
    public required string Identifier { get; init; }
    public bool ExceptIfNotFound { get; init; }
    public RegExPattern? RegExPattern { get; init; }
    public ScreenArea? ScreenArea { get; init; }
    public override string ToString() => Identifier;
}