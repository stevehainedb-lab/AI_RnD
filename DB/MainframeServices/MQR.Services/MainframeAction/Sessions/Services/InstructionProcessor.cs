using MQR.Services.Instructions.Models.Shared;
using MQR.Services.MainframeAction.Sessions.Abstractions;
using MQR.Services.Observability;
using Open3270.Interfaces;

namespace MQR.Services.MainframeAction.Sessions.Services;

internal sealed class InstructionProcessor : IInstructionProcessor
{
    public ITnEmulator Emulator { get; }
    private readonly IScreenWaiter _screenWaiter;
    private readonly IScreenInputWriter _inputWriter;
    private readonly INavigationExecutor _navigation;
    private readonly IScreenDataExtractor _dataExtractor;

    public InstructionProcessor(
        ITnEmulator emulator,
        IScreenWaiter screenWaiter,
        IScreenInputWriter inputWriter,
        INavigationExecutor navigation,
        IScreenDataExtractor dataExtractor)
    {
        Emulator = emulator;
        _screenWaiter = screenWaiter;
        _inputWriter = inputWriter;
        _navigation = navigation;
        _dataExtractor = dataExtractor;
    }

    public async Task ExecuteAsync(
        List<ProcessAction> actions,
        string task,
        QueryRunArgs queryRunArgs,
        Func<string, string>? getValueMethod,
        IMainframeIoLogger? mainframeIoLogger,
        CancellationToken cancellationToken = default)
    {
        foreach (var item in actions)
        {
            await DoProcessActionAsync(item, $"{task}-{item.Identifier}", queryRunArgs, getValueMethod, mainframeIoLogger, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(queryRunArgs.ShutdownReason)) break;
        }
    }

    private async Task DoProcessActionAsync(
        ProcessAction action,
        string task,
        QueryRunArgs queryRunArgs,
        Func<string, string>? getValueMethod,
        IMainframeIoLogger? log,
        CancellationToken ct)
    {
        if (action.EnabledWhen != null)
        {
            if (!EnabledProcessAction(action.EnabledWhen, queryRunArgs))
            {
                log?.LogImportantLine($"PROCESS ACTION : {action.Identifier} Not Enabled {action.EnabledWhen} : FALSE");
                return;
            }
        }

        log?.LogImportantLine($"PROCESS ACTION : {action.Identifier}");

        // Error checks first - will except if it finds any
        // Replace extension-based error marker check with waiter-based detection
        if (action.ErrorScreenIdentificationMarks is { Count: > 0 })
        {
            foreach (var identificationMarker in action.ErrorScreenIdentificationMarks)
            {
                if (await _screenWaiter.WaitForScreenAsync(identificationMarker, log, ct).ConfigureAwait(false))
                {
                    var reason = $"Error identification marker found: {identificationMarker.Identifier}";
                    log?.LogImportantLine(reason);
                    throw new InvalidOperationException(reason);
                }
            }
        }

        if (await _screenWaiter.WaitForScreensAsync(action.ScreenIdentificationMarks, log, ct).ConfigureAwait(false))
        {
            if (queryRunArgs.WaitForResponse) queryRunArgs.WaitForResponse = !action.NoPrintDataExpected;
            if (action.ScreenCaptureDataPoints.Count > 0)
            {
                var captures = await _dataExtractor.ExtractCapturePointsAsync(action.ScreenCaptureDataPoints, log, ct).ConfigureAwait(false);
                queryRunArgs.Captures.AddRange(captures);
            }

            if (action.ScreenInputs.Count > 0)
            {
                await _inputWriter.WriteInputsAsync(action.ScreenInputs, getValueMethod, log, ct).ConfigureAwait(false);
            }

            if (action.NavigationAction is not null)
            {
                var trans = await _navigation.DoNavigationAsync(action.NavigationAction, $"NavigationAction_{action.Identifier}", log, ct).ConfigureAwait(false);
                if (trans != null) queryRunArgs.Transactions.Add(trans);
            }

            await ExecuteAsync(action.ProcessActions, task, queryRunArgs, getValueMethod, log, ct).ConfigureAwait(false);
        }
        else
        {
            if (action.IsCoreAction)
            {
                queryRunArgs.ShutdownReason = $"Failed Core Action for {action.Identifier} due to Incorrect screen.";
                log?.LogImportantLine($"SHUTDOWN : {queryRunArgs.ShutdownReason}");
                throw new InvalidOperationException(queryRunArgs.ShutdownReason);
            }

            log?.LogImportantLine($"NO PROCESS ACTION FOR '{action.Identifier}' Screens Not Found");
        }

        log?.LogImportantLine($"PROCESS ACTION END : {action.Identifier}");

        static bool EnabledProcessAction(string actionEnabledWhen, QueryRunArgs args)
        {
            switch (actionEnabledWhen.ToUpper())
            {
                case null:
                    return true;
                case "PASSWORDEXPIRED":
                    if (args.Credential == null) throw new InvalidOperationException("PasswordExpired cannot be used with Credential");
                    if (args.PasswordChangeInterval == null) throw new InvalidOperationException("PasswordExpired cannot be used with PasswordChangeInterval");
                    return args.Credential.NeedsPasswordChanging(args.PasswordChangeInterval.Value);
                default:
                    throw new InvalidDataException("Unknown actionEnabledWhen: " + actionEnabledWhen);
            }
        }
    }
}