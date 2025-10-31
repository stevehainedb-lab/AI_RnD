namespace MQR.DataAccess.Entities;

public class RawOutput
{
    public Guid RequestId { get; init; }
    public required string RawMainframeOutput { get; init; }
}