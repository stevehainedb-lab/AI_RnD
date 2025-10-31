using MQR.Services.Instructions.Models.Queries;
using MQR.Services.Observability;
using Open3270.Interfaces;

namespace MQR.Services.MainframeAction.Sessions.Abstractions;

public interface ILogonRunner
{
    ITnEmulator Emulator { get; }
    Task<bool> DoLogonAsync(LogonInstructionSet instructionSet, QueryRunArgs queryRunArgs, IMainframeIoLogger? mainframeIoLogger, CancellationToken cancellationToken = default);
}