using MQR.DataAccess.Entities;

namespace MQR.Services.MainframeAction.Sessions;

public class QueryRunArgs
{
    public Task<string>? GetNewPassword { get; set; } 
    public LogonCredential? Credential { get; set; } 
    public TimeSpan? PasswordChangeInterval { get; set; }
    public bool WaitForResponse { get; set; } = true;
    public string NoResponseText { get; set; } = string.Empty;
    public string ShutdownReason { get; set; } = string.Empty;
    public bool RevokeCredential { get; set; }
    public string? NewPassword { get; set; }
    public List<(string Key, string Value)> Captures { get; set; } = [];
    public List<TransactionData> Transactions { get; set; } = [];
    public string TopsAltName { get; set; }
}