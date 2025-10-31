namespace MQR.DataAccess.Entities;

public class ParsedOutput
{
    public Guid RequestId { get; init; }
    public required string ParsedMainframeJson { get; init; }
}