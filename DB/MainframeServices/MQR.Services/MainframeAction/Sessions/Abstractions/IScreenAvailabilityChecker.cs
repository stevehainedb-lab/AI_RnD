using MQR.Services.Instructions.Models.Shared;
using MQR.Services.Observability;
using Open3270.Interfaces;

namespace MQR.Services.MainframeAction.Sessions.Abstractions;

public interface IScreenAvailabilityChecker
{
    ITnEmulator Emulator { get; }
    Task<(bool Ok, string? Reason, List<(string Key, string Value)> Captures)> CheckAsync(ProcessAction logonInstruction, TimeSpan serverMainframeTimeTolerance, IMainframeIoLogger? mainframeIoLogger, CancellationToken cancellationToken = default);
}