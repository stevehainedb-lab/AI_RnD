using MQR.Services.Instructions.Models.Queries;
using MQR.Services.MainframeAction.Sessions.Abstractions;
using MQR.Services.Observability;
using Open3270.Interfaces;

namespace MQR.Services.MainframeAction.Sessions.Services;

internal sealed class LogonRunner : ILogonRunner
{
    public ITnEmulator Emulator { get; }
    private readonly IScreenWaiter _screenWaiter;
    private readonly IInstructionProcessor _instructionProcessor;
    private readonly ISuccessConditionEvaluator _successEvaluator;

    public LogonRunner(ITnEmulator emulator, IScreenWaiter screenWaiter, IInstructionProcessor instructionProcessor, ISuccessConditionEvaluator successEvaluator)
    {
        Emulator = emulator;
        _screenWaiter = screenWaiter;
        _instructionProcessor = instructionProcessor;
        _successEvaluator = successEvaluator;
    }

    public async Task<bool> DoLogonAsync(LogonInstructionSet instructionSet, QueryRunArgs queryRunArgs, IMainframeIoLogger? mainframeIoLogger, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        mainframeIoLogger?.LogImportantLine("DoLogon: " + instructionSet);

        var instruction = instructionSet.LogonInstruction;
        if (!await _screenWaiter.WaitForScreensAsync(instruction.ScreenIdentificationMarks, mainframeIoLogger, cancellationToken).ConfigureAwait(false))
        {
            queryRunArgs.ShutdownReason = "Screen did not initialise";
            return false;
        }

        await _instructionProcessor.ExecuteAsync(
            instruction.ProcessActions,
            "Logon",
            queryRunArgs,
            UserInputResolver,
            mainframeIoLogger,
            cancellationToken).ConfigureAwait(false);

        if (!await _successEvaluator.CheckAsync(instruction.SuccessCondition, mainframeIoLogger, cancellationToken).ConfigureAwait(false))
        {
            queryRunArgs.ShutdownReason = "Failure of Success Condition.";
            return false;
        }

        mainframeIoLogger?.LogImportantLine("DoLogon Complete: " + instructionSet);
        return true;

        string UserInputResolver(string s)
        {
            return s.ToUpper() switch
            {
                "NEWPASSWORD" => queryRunArgs.GetNewPassword?.Result ?? throw new InvalidOperationException("GetNewPassword not in queryRunArgs"),
                "PASSWORD" => queryRunArgs.Credential?.Password ?? throw new InvalidOperationException("Credential not in queryRunArgs"),
                "USERNAME" => queryRunArgs.Credential?.UserName ?? throw new InvalidOperationException("Credential not in queryRunArgs"),
                _ => throw new InvalidOperationException("Unknown string Key: " + s)
            };
        }
    }
}