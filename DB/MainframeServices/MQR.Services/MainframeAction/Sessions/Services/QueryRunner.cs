using MQR.Services.Instructions.Models.Queries;
using MQR.Services.Instructions.Models.Shared;
using MQR.Services.MainframeAction.Sessions.Abstractions;
using MQR.Services.Observability;
using MQR.WebAPI.ServiceModel;
using Open3270.Interfaces;

namespace MQR.Services.MainframeAction.Sessions.Services;

internal sealed class QueryRunner : IQueryRunner
{
    public ITnEmulator Emulator { get; }
    private readonly IInstructionProcessor _instructionProcessor;

    public QueryRunner(ITnEmulator emulator, IInstructionProcessor instructionProcessor)
    {
        Emulator = emulator;
        _instructionProcessor = instructionProcessor;
    }

    public Task DoProcessActionsAsync(List<ProcessAction> actions, string task, QueryRunArgs queryRunArgs, Func<string, string>? getValueMethod, IMainframeIoLogger? mainframeIoLogger, CancellationToken cancellationToken = default)
    {
        return _instructionProcessor.ExecuteAsync(actions, task, queryRunArgs, getValueMethod, mainframeIoLogger, cancellationToken);
    }

    public async Task<bool> DoQueryAsync(QueryRequest request, QueryInstructionSet queryInstructionSet, QueryRunArgs queryRunArgs, IMainframeIoLogger? mainframeIoLogger = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        mainframeIoLogger?.LogImportantLine("DoQuery : " + request.QueryInstructionSet);
        try
        {
            request.SessionUsed = queryRunArgs.TopsAltName;

            await _instructionProcessor.ExecuteAsync(
                queryInstructionSet.ProcessActions,
                $"Query:{queryInstructionSet.Identifier}",
                queryRunArgs,
                s => request.Parameters.GetPramValue(s),
                mainframeIoLogger,
                cancellationToken).ConfigureAwait(false);

            mainframeIoLogger?.LogImportantLine("DoQuery Complete: " + request.QueryInstructionSet);
            return true;
        }
        catch (Exception e)
        {
            queryRunArgs.ShutdownReason = $"FailedQuery '{request.QueryInstructionSet} {e.Message}.";
            queryRunArgs.WaitForResponse = false;
            mainframeIoLogger?.LogImportantLine(queryRunArgs.ShutdownReason);
            mainframeIoLogger?.LogImportantLine("DoQuery Failed: " + request.QueryInstructionSet);
            return false;
        }
    }
}