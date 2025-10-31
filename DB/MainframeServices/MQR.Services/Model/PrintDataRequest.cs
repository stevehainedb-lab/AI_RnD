namespace MQR.Services.Model;

public class PrintDataRequest
{
    public required string PrintLata { get; init; }
    public required string PrintSessionId { get; init; }
    public required DateTime ReceivedTimeUtc { get; init; }
    public required string PrintId { get; init; }
    public required string RawData { get; init; }
}