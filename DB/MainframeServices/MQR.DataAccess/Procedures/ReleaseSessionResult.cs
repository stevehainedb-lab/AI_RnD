namespace MQR.DataAccess.Entities;

/// <summary>
/// Result returned from the sp_ReleaseSessionAndUpdateRawOutput stored procedure.
/// </summary>
public class ReleaseSessionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string TopsAlternateName { get; set; } = string.Empty;
    public Guid? RequestId { get; set; }
    public DateTime? PreviousLockTakenUtc { get; set; }
    public int RowsAffected { get; set; }
}
