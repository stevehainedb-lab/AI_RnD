namespace MQR.Services.Queues;

/// <summary>
/// Represents a message emitted from MPS.
/// </summary>
/// <param name="SessionId">The ID of the mainframe session.</param>
/// <param name="RawMainframeResponse">The unparsed text produced by the mainframe.</param>
public record PrintResult(string SessionId, string RawMainframeResponse);