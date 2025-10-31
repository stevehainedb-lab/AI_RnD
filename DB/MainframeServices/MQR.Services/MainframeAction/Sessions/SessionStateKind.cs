namespace MQR.Services.MainframeAction.Sessions
{
    public enum SessionStateKind
    {
        Inital,
        CheckingAvailability,
        LoggingOn,
        ReadyForCommand,
        AwaitingResponse,
        Processing,
        ShutDownInProgress
    }
}