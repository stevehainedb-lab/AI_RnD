using MQR.Services.MainframeAction.Sessions.Abstractions;

namespace MQR.Services.MainframeAction.Sessions.Models;

public sealed class SessionEmulatorServices
{
    public required ILogonRunner LogonRunner { get; init; }
    public required IQueryRunner QueryRunner { get; init; }
    public required IScreenWaiter ScreenWaiter { get; init; }
    public required IScreenInputWriter ScreenInputWriter { get; init; }
    public required INavigationExecutor NavigationExecutor { get; init; }
    public required IScreenDataExtractor ScreenDataExtractor { get; init; }
    public required IInstructionProcessor InstructionProcessor { get; init; }
    public required ISuccessConditionEvaluator SuccessConditionEvaluator { get; init; }
    public required IScreenAvailabilityChecker ScreenAvailabilityChecker { get; init; }
    public required IEmulatorGate EmulatorGate { get; init; }
}
