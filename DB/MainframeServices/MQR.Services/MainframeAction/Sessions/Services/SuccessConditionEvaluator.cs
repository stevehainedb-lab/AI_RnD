using MQR.Services.Instructions.Models.Shared;
using MQR.Services.MainframeAction.Sessions.Abstractions;
using MQR.Services.Observability;
using Open3270.Interfaces;

namespace MQR.Services.MainframeAction.Sessions.Services;

internal sealed class SuccessConditionEvaluator : ISuccessConditionEvaluator
{
    public ITnEmulator Emulator { get; }
    private readonly IScreenWaiter _screenWaiter;

    public SuccessConditionEvaluator(ITnEmulator emulator, IScreenWaiter screenWaiter)
    {
        Emulator = emulator;
        _screenWaiter = screenWaiter;
    }

    public Task<bool> CheckAsync(SuccessCondition? condition, IMainframeIoLogger? mainframeIoLogger, CancellationToken cancellationToken = default)
    {
        if (condition == null) return Task.FromResult(true);
        return _screenWaiter.WaitForScreensAsync(condition.ScreenIdentificationMarks, mainframeIoLogger, cancellationToken);
    }
}