using MQR.Services.Instructions.Models.Shared;
using MQR.Services.Observability;
using Open3270.Interfaces;

namespace MQR.Services.MainframeAction.Sessions.Abstractions;

public interface ISuccessConditionEvaluator
{
    ITnEmulator Emulator { get; }
    Task<bool> CheckAsync(SuccessCondition? condition, IMainframeIoLogger? mainframeIoLogger, CancellationToken cancellationToken = default);
}