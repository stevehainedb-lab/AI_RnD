using MQR.WebAPI.ServiceModel;

namespace MQR.Services.ServiceModel;

public record QueueRequest(
    Guid RequestId,
    QueryRequest Req);