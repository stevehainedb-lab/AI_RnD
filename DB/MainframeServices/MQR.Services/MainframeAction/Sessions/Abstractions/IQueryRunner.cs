using MQR.Services.Instructions.Models.Queries;
using MQR.Services.Instructions.Models.Shared;
using MQR.Services.MainframeAction.Sessions;
using MQR.Services.Observability;
using MQR.WebAPI.ServiceModel;
using Open3270.Interfaces;

namespace MQR.Services.MainframeAction.Sessions.Abstractions;

public interface IQueryRunner
{
    ITnEmulator Emulator { get; }
    Task DoProcessActionsAsync(List<ProcessAction> actions, string task, QueryRunArgs queryRunArgs, Func<string, string>? getValueMethod, IMainframeIoLogger? mainframeIoLogger, CancellationToken cancellationToken = default);
    Task<bool> DoQueryAsync(QueryRequest request, QueryInstructionSet queryInstructionSet, QueryRunArgs queryRunArgs, IMainframeIoLogger? mainframeIoLogger = null, CancellationToken cancellationToken = default);
}