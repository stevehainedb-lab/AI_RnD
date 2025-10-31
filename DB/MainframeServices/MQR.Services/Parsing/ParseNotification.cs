namespace MQR.Services.Queues;

/// <summary>
/// Notification sent when a print result is ready to be parsed.
/// </summary>
/// <param name="RequestId">The unique identifier for this request.</param>
/// <param name="Result">The raw print result from the mainframe.</param>
/// <param name="ParseInstructionSetIdentifier">The identifier of the parse instruction set to use.</param>
/// <param name="RequestSessionIdentifier">The identifier of the request session for logging purposes.</param>
/// <param name="MFResponseTime">The time the mainframe response was received.</param>
/// <param name="ParseSplitRegex">Optional regex pattern to split the print response (defaults to form feed \f).</param>
public record ParseNotification(
    Guid RequestId,
    PrintResult Result,
    string ParseInstructionSetIdentifier,
    string? RequestSessionIdentifier = null,
    DateTime MFResponseTime = default,
    string? ParseSplitRegex = null);