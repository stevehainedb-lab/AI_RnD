namespace MQR.DataAccess.Entities;

public class TransactionData
{
    public string Action { get; set; } = string.Empty;
    public string? ActionContext { get; set; }
    public string Credential { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string CallingApplication { get; set; }
    public string Tags { get; set; } = string.Empty;

}