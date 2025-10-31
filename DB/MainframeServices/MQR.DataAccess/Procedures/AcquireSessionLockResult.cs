namespace MQR.DataAccess.Entities;

/// <summary>
/// Result returned from the sp_AcquireSessionLock stored procedure.
/// </summary>
public class AcquireSessionLockResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public Guid RequestId { get; set; }
    public DateTimeOffset? LockTakenUtc { get; set; }
    public DateTimeOffset? PreviousLockTakenUtc { get; set; }
    public Guid? PreviousRequestId { get; set; }
    public bool WasAlreadyLocked { get; set; }
}
