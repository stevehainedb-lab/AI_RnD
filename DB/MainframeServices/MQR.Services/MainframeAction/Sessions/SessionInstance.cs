using System.Data;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MQR.DataAccess.Entities;
using MQR.Services.Credentials;
using MQR.Services.Instructions.Models.Queries;
using MQR.Services.Model;
using MQR.Services.Observability;
using System.Diagnostics;
using MQR.WebAPI.ServiceModel;
using MQR.Services.MainframeAction.Sessions.Abstractions;
using MQR.Services.MainframeAction.Sessions.Models;

using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using ScreenIdentificationMark = MQR.Services.Instructions.Models.Shared.ScreenIdentificationMark;

using Open3270;
using Open3270.Interfaces;

namespace MQR.Services.MainframeAction.Sessions;

public sealed class SessionInstance : IAudit, IAsyncDisposable
{
    
    private readonly ILogonCredentialProvider _credentialProvider;
    private readonly CancellationTokenSource _cts = new();
    
    //private readonly MQRDbContext _dbContext;
    private readonly ITnEmulator _emulator;
    private readonly Lock _emulatorLock = new();
    private readonly Task<QueryRunArgs?> _initTask;
    private readonly ILogger _logger;
    private readonly IMainframeIoLogger _mainframeIoLogger;
    private readonly MqrMetrics _metrics;
    private readonly IOptions<MqrConfig> _mqrConfig;
    private readonly ISessionProvider _sessionProvider;
    private readonly ILogonRunner _logonRunner;
    private readonly IQueryRunner _queryRunner;
    private readonly IScreenWaiter _screenWaiter;
    private readonly IScreenAvailabilityChecker _availabilityChecker;
    private readonly IEmulatorGate _emulatorGate;
    private readonly IScreenDataExtractor _dataExtractor;
    
    public SessionInstance(
        string sessionId,
        LogonInstructionSet instructionSet,
        ITnEmulator emulator,
        SessionEmulatorServices emulatorServices,
        ILogonCredentialProvider credentialProvider,
        ISessionProvider sessionProvider,
        IOptions<MqrConfig> mqrConfig,
        MqrMetrics mqrMetrics,
        IMainframeIoLogger mainframeIoLogger,
        ILogger logger)
    {
        LogonInstructionSet = instructionSet ?? throw new ArgumentNullException(nameof(instructionSet));
        SessionId = sessionId;
        TopsAltName = "TBC";
        TopsName = "TBC";

        _credentialProvider = credentialProvider;
        _sessionProvider = sessionProvider;
        _mqrConfig = mqrConfig;
        _mainframeIoLogger = mainframeIoLogger;
        _logger = logger;
        _emulator = emulator;
        _metrics = mqrMetrics;

        if (emulatorServices is null) throw new ArgumentNullException(nameof(emulatorServices));
        _logonRunner = emulatorServices.LogonRunner;
        _queryRunner = emulatorServices.QueryRunner;
        _screenWaiter = emulatorServices.ScreenWaiter;
        _availabilityChecker = emulatorServices.ScreenAvailabilityChecker;
        _emulatorGate = emulatorServices.EmulatorGate;
        _dataExtractor = emulatorServices.ScreenDataExtractor;
        
        // Start async initialisation immediately
        _initTask = Task.Run(() => InitialiseEmulatorAsync(_cts.Token), _cts.Token);
    }
    
    public string SessionId { get; init; }
    public string TopsName { get; private set; }
    public string TopsAltName { get; private set; }
    
    private LogonCredential? Credential { get; set; }
    private LogonInstructionSet LogonInstructionSet { get; }
    private SessionStateKind State { get; set; } = SessionStateKind.Inital;
    private DateTime LastStateChangeUtc { get; set; } = DateTime.UtcNow;

    private bool TopsNamesCaptured => !string.IsNullOrEmpty(TopsName) && !string.IsNullOrEmpty(TopsAltName);
    
    private List<ScreenIdentificationMark> HomeScreen => LogonInstructionSet.LogonInstruction.SuccessCondition.ScreenIdentificationMarks;
    
    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        _cts.Dispose();
    }
    
    private async Task<QueryRunArgs?> InitialiseEmulatorAsync(CancellationToken ct)
    {
        QueryRunArgs? queryRunArgs = null;
        _logger.LogInformation("Initialising Emulator for {Session}", ToString());
        
        Credential = await _credentialProvider.GetValidCredentialAsync(LogonInstructionSet.CredentialPool, ct) ?? 
                     throw new DataException($"Could not obtain Logon Credential for {LogonInstructionSet.Identifier}.");
        _logger.LogInformation("Using Credential {Credential} for session {SessionId}", Credential.UserName, SessionId);
        
        try
        {
            lock (_emulatorLock)
            {
                _emulator.Config.FastScreenMode = true;
                _emulator.Config.AlwaysSkipToUnprotected = true;
                _emulator.Config.IgnoreSequenceCount = true;
                _emulator.Config.SubmitAllKeyboardCommands = true;
                _emulator.Config.ThrowExceptionOnLockedScreen = true;
                
                _emulator.Config.AlwaysRefreshWhenWaiting = false;
                _emulator.Config.IdentificationEngineOn = false;
                _emulator.Config.LockScreenOnWriteToUnprotected = false;
                _emulator.Config.RefuseTn3270E = false;
                
                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    _emulator.Audit = this;
                    _emulator.Debug = true;
                }
                else
                {
                    _emulator.Debug = false;
                }
                _emulator.Config.HostLu = SessionId;
                _emulator.Config.DefaultTimeout = _mqrConfig.Value.Tn3270EmulatorDefaultTimeout;
                _emulator.Config.HostName = LogonInstructionSet.LogonConnection.HostAddress;
                _emulator.Config.HostPort = LogonInstructionSet.LogonConnection.HostPort;
                _emulator.Config.TermType = TerminalTypeFormatter.Format(LogonInstructionSet.LogonConnection.TerminalDeviceType.ToString());
            }

            _logger.LogDebug("Connecting Emulator for {Session}", ToString());

            await _emulator.ConnectAsync(ct);
            await _emulator.WaitForHostSettleAsync(
                (int)_mqrConfig.Value.ScreenCheckInterval.TotalMilliseconds,
                (int)_mqrConfig.Value.HostSettleTimeout.TotalMilliseconds, ct);

            _logger.LogInformation("Connected Emulator for {Session}. CheckingAvailability.", ToString());
            
            SetState(SessionStateKind.CheckingAvailability);
            var availability = await _availabilityChecker.CheckAsync(LogonInstructionSet.LogonInstruction, _mqrConfig.Value.ServerMainframeTimeTolerance, _mainframeIoLogger, ct);
            AddCapturedDataToLogonCaptures(availability.Captures);
            if (!availability.Ok)
            {
                Shutdown($"Check Availability Failed: {availability.Reason}");
            }
            else
            {
                var timeShift = availability.Captures.Single(c => c.Key == "ServerToMfTimeShift").Value;
                _sessionProvider.UpdateTimeShift(TimeSpan.Parse(timeShift));
            }

            queryRunArgs = new QueryRunArgs
            {
                TopsAltName = TopsAltName,
                Credential = Credential,
                GetNewPassword = _credentialProvider.GenerateNewPasswordAsync(CancellationToken.None),
                PasswordChangeInterval = _mqrConfig.Value.PasswordChangeInterval
            };
            
            await DoLogonAsync(queryRunArgs, ct);
            if (!string.IsNullOrEmpty(queryRunArgs.ShutdownReason)) Shutdown($"Logon Requested Shutdown: {queryRunArgs.ShutdownReason}");

            //short delay to ensure we are all connected
            await Task.Delay(200, ct);
        }
        catch (TnHostException ex) when (ex.Message.Contains("Unable to resolve host", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogCritical("Unable to resolve host : {Host}", LogonInstructionSet.LogonConnection.HostAddress);
        }
        catch (OperationCanceledException)
        {
            // normal during shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while initialising Emulator!");
            Shutdown(FormatException("Exception occurred while initialising Emulator!", ex));
        }
        return queryRunArgs;
    }

    private void AddCapturedDataToLogonCaptures(List<(string Key, string Value)> captures)
    {
        foreach (var capture in captures)
        {
            switch (capture.Key)
            {
                case "TopsName":
                    TopsName = capture.Value;
                    break;
                case "TopsAltName":
                    TopsAltName = capture.Value;
                    break;
                default:
                    _emulator.WriteAudit($"Captured Data: {capture.Key} = {capture.Value} but not stored");
                    break;
            }
        }
    }

    public async Task<QueryRunArgs?> WaitForInitAsync(CancellationToken stoppingToken = default)
    {
        await Task.WhenAny(_initTask, Task.Delay(Timeout.Infinite, stoppingToken));
        return _initTask.Result;
    }

    private void Shutdown(string reason)
    {
        _sessionProvider.ShutdownSessionsAsync(reason);
    }

    public async Task TerminateSessionAsync(string reason, CancellationToken stoppingToken = default)
    {
        try
        {
            _mainframeIoLogger.LogImportantLine($"TERMINATE : {reason}");

            await _cts.CancelAsync();
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await WaitForInitAsync(timeoutCts.Token);
            
            lock (_emulatorLock)
            {
                _emulator.Debug = false;
                _emulator.Audit = null;
                _emulator.Close();
            }
        }
        catch (Exception e)
        {
            _logger.LogError($"Exception shutting down - {e}", e);
        }
    }

    public bool IsHealthy(out string reason)
    {
        reason = string.Empty;
        if (LastStateChangeUtc.AddSeconds(30) >= DateTime.UtcNow) return true;

        switch (State)
        {
            case SessionStateKind.AwaitingResponse:
            case SessionStateKind.ReadyForCommand:
                if (_emulator.IsConnected)
                {
                    if (!_screenWaiter.WaitForScreensAsync(HomeScreen, _mainframeIoLogger, default).GetAwaiter().GetResult())
                    {
                        var screen = _dataExtractor.GetVisibleScreenAsync(_mainframeIoLogger).GetAwaiter().GetResult();
                        reason = $"Home Screen Incorrect {screen}";
                        _logger.LogWarning(reason);
                    }
                }
                else
                {
                    reason = "Emulator Not Connected";
                    _logger.LogWarning(reason);
                }

                break;

            case SessionStateKind.CheckingAvailability:
            case SessionStateKind.Inital:
            case SessionStateKind.LoggingOn:
                reason = LastStateChangeUtc.AddMinutes(5) < DateTime.UtcNow
                    ? $"Emulator in state {State} for {(DateTime.UtcNow - LastStateChangeUtc).TotalSeconds} seconds."
                    : string.Empty;
                break;

            case SessionStateKind.Processing:
            case SessionStateKind.ShutDownInProgress:
                reason = string.Empty;
                break;

            default:
                throw new InvalidConstraintException($"State unrecognised : {State}");
        }

        return reason == string.Empty;
    }

    private void SetState(SessionStateKind value)
    {
        if (State == value || State == SessionStateKind.ShutDownInProgress) return;
        LastStateChangeUtc = DateTime.UtcNow;
        State = value;
        _logger.LogDebug("State set to {State} on {Session}", value, ToString());
    }
    
    private async Task DoLogonAsync(QueryRunArgs queryRunArgs, CancellationToken cancellationToken = default)
    {
        bool successfulLogon;
        try
        {
            if (Credential is null) throw new InvalidOperationException("Credential is null");

            try
            {
                if (await _screenWaiter.WaitForScreensAsync(HomeScreen, _mainframeIoLogger, cancellationToken))
                {
                    _logger.LogInformation("{Session} Credential {Cred} is logging on", SessionId, Credential.UserName);
                    SetState(SessionStateKind.LoggingOn);
                    successfulLogon = await _logonRunner.DoLogonAsync(
                        LogonInstructionSet,
                        queryRunArgs,
                        _mainframeIoLogger,
                        cancellationToken);

                    AddCapturedDataToLogonCaptures(queryRunArgs.Captures);

                    if (queryRunArgs.NewPassword != null)
                    {
                        await _credentialProvider.UpdateCredentialPasswordAsync(Credential, queryRunArgs.NewPassword, cancellationToken);
                    }

                    if (queryRunArgs.RevokeCredential)
                    {
                        await _credentialProvider.RevokeCredentialAsync(Credential, cancellationToken);
                    }

                    if (!TopsNamesCaptured)
                    {
                        queryRunArgs.ShutdownReason = "Could not capture TOPSNames.";
                    }
                }
                else
                {
                    queryRunArgs.ShutdownReason = "Could not Identify Initial Screen.";
                    successfulLogon = false;
                }
            }
            finally
            {
                await _credentialProvider.UnlockCredentialAsync(Credential, cancellationToken);
            }

            if (successfulLogon)
            {
                _logger.LogInformation("{Session} logged on successfully", ToString());
                SetState(SessionStateKind.ReadyForCommand);
            }
            else
            {
                throw new InvalidOperationException($"Failed to logon because {queryRunArgs.ShutdownReason}");
            }
        }
        finally
        {
            _mainframeIoLogger.ClearCurrentTrace();
        }
    }

    public async Task<QueryRunArgs> RunQueryAsync(QueryRequest request, QueryInstructionSet queryInstructionSet, CancellationToken cancellationToken = default)
    {
        using var activity = MqrTracing.ActivitySource.StartActivity("Session.RunQuery", ActivityKind.Internal);
        activity?.SetTag("mqr.request.id", request.RequestId);
        activity?.SetTag("mqr.session.id", SessionId);
        activity?.SetTag("mqr.query.instructionSet", request.QueryInstructionSet);
        var swTotal = Stopwatch.StartNew();

        var queryRunResult = new QueryRunArgs
        {
            TopsAltName = TopsAltName
        };

        try
        {
            using var loggerScope = _logger.BeginScope(request.RequestId);

            if (!await _screenWaiter.WaitForScreensAsync(HomeScreen, _mainframeIoLogger, cancellationToken))
            {
                queryRunResult.ShutdownReason = "Incorrect Screen before the query ran.";
                activity?.AddEvent(new ActivityEvent("precheck.homeScreen.failed"));
            }
            else
            {
                if (State != SessionStateKind.ReadyForCommand)
                    throw new InvalidOperationException($"Session: {SessionId} state is currently {State}. Cannot run query!");

                // Serialize emulator use for the critical section using async gate
                var gate = await _emulatorGate.AcquireAsync(cancellationToken);
                using (gate)
                {
                    SetState(SessionStateKind.Processing);
                    var ok = await _queryRunner.DoQueryAsync(request, queryInstructionSet, queryRunResult, _mainframeIoLogger, cancellationToken).ConfigureAwait(false);
                    activity?.SetTag("mqr.query.ok", ok);
                    if (ok) _metrics.QueriesRun.Add(1);
                } // end gate
            }

            if (string.IsNullOrEmpty(queryRunResult.ShutdownReason))
            {
                _logger.LogTrace("3270Session:" + SessionId + " Credential: " + Credential?.UserName +
                                 " Completed Running Query: " + request +
                                 " from InstructionSet:" + LogonInstructionSet.Identifier);

                SetState(queryRunResult.WaitForResponse ? SessionStateKind.AwaitingResponse : SessionStateKind.ReadyForCommand);
                activity?.SetStatus(ActivityStatusCode.Ok);
            }
            else
            {
                _logger.LogTrace("3270Session:{SessionID} Credential:{Credential} Failed Running Query:{Query} from InstructionSet:{LogonInstruction}", SessionId,
                    Credential?.UserName, request.QueryInstructionSet, LogonInstructionSet.Identifier);

                // Something has gone wrong, and a print may have started:
                await Task.Delay(5000, cancellationToken);

                Shutdown($"Do Query Requested Shutdown - {queryRunResult.ShutdownReason}");
                activity?.SetStatus(ActivityStatusCode.Error, queryRunResult.ShutdownReason);
                throw new InvalidOperationException($"FailureReason: {queryRunResult.ShutdownReason} Query: {request.QueryInstructionSet}");
            }
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
            {
                { "exception.type", ex.GetType().FullName },
                { "exception.message", ex.Message }
            }));
            throw;
        }
        finally
        {
            swTotal.Stop();
            MqrTracing.HQueryDurationMs.Record(swTotal.Elapsed.TotalMilliseconds, new KeyValuePair<string, object?>("session", SessionId));
            _mainframeIoLogger.ClearCurrentTrace();
        }

        return queryRunResult;
    }
    
    private string FormatException(string message, Exception? ex = null)
    {
        var result = new StringBuilder();
        result.AppendLine(message);
        result.AppendLine(string.Empty);
        if (ex is not null)
        {
            result.AppendLine("EXCEPTION:");
            result.AppendLine(ex.ToString());
            result.AppendLine(string.Empty);
        }

        result.AppendLine("SCREEN:");
        result.AppendLine(_mainframeIoLogger.GetScreenTrace(_emulator.CurrentScreen.Dump()));
        return result.ToString();
    }
    
    public override string ToString()
    {
        return $"ID:{SessionId} TOPSName:{TopsName} TOPSAltName:{TopsAltName} Credential:{Credential?.UserName} State:{State}";
    }
    
    private readonly StringBuilder _loggerBuffer = new StringBuilder();
    public void Write(string text)
    {
        _loggerBuffer.Append(text);
    }

    public void WriteLine(string text)
    {
        if (_loggerBuffer.Length > 0)
        {
            _logger.LogTrace(_loggerBuffer.ToString());
            _loggerBuffer.Clear();
        }
        _logger.LogTrace(text);
    }
}