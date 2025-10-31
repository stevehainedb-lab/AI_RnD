namespace MQR.DataAccess.Entities;

public class RequestSession
{
    public Guid RequestId { get; init; }

    public string? SessionId { get; set; }

    public string? ClientTrackingId { get; set; }

    public string? ClientRequest { get; set; }

    public RequestStatus Status { get; set; } = RequestStatus.Started;

    public RawOutput? RawMainframeOutput { get; set; }

    public ParsedOutput? ParsedMainframeOutput { get; set; }

    public DateTime CreatedOnUtc { get; set; }
    public ActiveSession? ActiveSession { get; set; }
}