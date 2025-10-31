namespace MQR.DataAccess.Entities;

public enum RequestStatus
{
    Started,
    InProgress,
    InvokingMainframeQuery,
    ParsingMainframeResponse,
    Complete,
    Failed,
    
}