

using MQR.WebAPI.ServiceModel;

namespace MQR.Services.MainframeAction;

public class MainframeActionNotification
{
    public required QueryRequest Request { get; set; }
    public required Guid RequestId { get; set; }
}