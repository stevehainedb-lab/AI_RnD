using MQR.Services.Instructions.Models.Shared;
using MQR.Services.Observability;
using Open3270.Interfaces;

namespace MQR.Services.MainframeAction.Sessions.Abstractions;

public interface IInstructionProcessor
{
    ITnEmulator Emulator { get; }
    Task ExecuteAsync(
        List<ProcessAction> actions,
        string task,
        QueryRunArgs queryRunArgs,
        Func<string, string>? getValueMethod,
        IMainframeIoLogger? mainframeIoLogger,
        CancellationToken cancellationToken = default);
}