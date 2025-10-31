namespace MQR.Services.ServiceModel;

public enum Status
{
    NotFound,
    Ok,
    Error,
    Wait
}

public class StatusResponse
{
    public Status Status { get; set; }
    public string Message { get; set; }
}