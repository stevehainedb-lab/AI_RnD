using System.Collections.Concurrent;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQR.DataAccess.Context;
using MQR.DataAccess.Entities;
using MQR.Services.Credentials;
using MQR.Services.Model;
using MQR.Services.Observability;
using Open3270;
using System.Diagnostics;

namespace MQR.Services.MainframeAction.Sessions;

public interface ISessionProvider
{
    Task CreateSessionInstance(LogonInstructionSet instructionSet, CancellationToken cancellationToken = default);
    Task<SessionInstance> AcquireSessionLockAsync(string sessionPool, Guid requestId, CancellationToken cancellationToken = default);
    Task ReleaseSessionLockAsync(string sessionId, CancellationToken cancellationToken = default);
    void UpdateTimeShift(TimeSpan diff);
    Task ShutdownSessionsAsync(string reason, CancellationToken cancellationToken = default);
    Task<List<SessionPool>> GetSessionPools(CancellationToken cancellationToken = default);
    Task SetupSessionPoolsAsync(List<InstructionSetProvider.SessionPoolInfo> instructionSets, CancellationToken cancellationToken = default);
}

public class SessionProvider(
    IOptions<MqrConfig> config,
    ILogonCredentialProvider logonCredentialProvider,
    IDbContextFactory<MQRDbContext> dbContextFactory,
    MqrMetrics mqrMetrics,
    IMainframeIoLogger mainframeIoLogger,
    MQR.Services.MainframeAction.Sessions.Abstractions.ISessionEmulatorServicesFactory emulatorServicesFactory,
    ILoggerFactory loggerFactory,
    ILogger<SessionProvider> logger)
    : ISessionProvider
{
    private readonly ConcurrentDictionary<string, SessionPool> _sessionPools = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _instanceName = Guid.NewGuid().ToString();
    public async Task CreateSessionInstance(LogonInstructionSet instructionSet, CancellationToken cancellationToken = default)
    {
        var logger = loggerFactory.CreateLogger($"{nameof(SessionInstance)}_{instructionSet.Identifier}");
        
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var loginRequestId = Guid.NewGuid();
        var sessionLockResult = await dbContext.AcquireSessionLockAsync(_instanceName, instructionSet.Identifier, Guid.Empty, null, cancellationToken);
        var sessionId = sessionLockResult.SessionId;
        var session = await dbContext.ActiveSessions.FindAsync(sessionId, cancellationToken);
        if (session is null)
        {
            session = new ActiveSession
            {
                SessionId = sessionId,
                RequestId = loginRequestId,
                LockTakenUtc = DateTime.UtcNow,
                TopsAlternateName = "TBC-Login"
            };
            dbContext.ActiveSessions.Add(session);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        
        var tnEmulator = new TnEmulator();
        var emulatorServices = emulatorServicesFactory.Create(tnEmulator);
        var sessionInstance = new SessionInstance(
            sessionId,
            instructionSet,
            tnEmulator,
            emulatorServices,
            logonCredentialProvider,
            this,
            config,
            mqrMetrics,
            mainframeIoLogger,
            logger);

        var queryRunResult = await sessionInstance.WaitForInitAsync(cancellationToken);
        if (queryRunResult != null) dbContext.TransactionData.AddRange(queryRunResult.Transactions);
        session.TopsAlternateName = sessionInstance.TopsAltName;
        await dbContext.SaveChangesAsync(cancellationToken);
        await ReleaseSessionLockAsync(sessionId, cancellationToken);
        if (!_sessionPools.TryGetValue(instructionSet.Identifier, out var pool))
        {
            logger.LogError("CreateSessionInstance failed: Session pool not initialized {SessionPool}", instructionSet.Identifier);
            throw new InvalidOperationException($"Session pool '{instructionSet.Identifier}' not initialized");
        }
        pool.TryAdd(sessionInstance.SessionId, sessionInstance);
    }

    public async Task<SessionInstance> AcquireSessionLockAsync(string sessionPool, Guid requestId, CancellationToken cancellationToken = default)
    {
        using var activity = MqrTracing.ActivitySource.StartActivity("Session.AcquireLock", ActivityKind.Internal);
        activity?.SetTag("mqr.session.pool", sessionPool);
        activity?.SetTag("mqr.request.id", requestId);

        if (!_sessionPools.TryGetValue(sessionPool, out var pool))
        {
            logger.LogError("AcquireSessionAsync failed: Session pool not initialized {SessionPool}", sessionPool);
            activity?.SetStatus(ActivityStatusCode.Error, "Session pool not initialized");
            throw new InvalidOperationException($"Session pool '{sessionPool}' not initialized");
        }

        await using var dbcontext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        logger.LogInformation("AcquireSessionAsync {SessionPool}", sessionPool);

        bool decremented = false;
        var sw = Stopwatch.StartNew();
        try
        {
            var sessionLockInfo = await dbcontext.AcquireSessionLockAsync(_instanceName, sessionPool, requestId, null, cancellationToken);
            sw.Stop();
            MqrTracing.HSessionLockWaitMs.Record(sw.Elapsed.TotalMilliseconds, new KeyValuePair<string, object?>("pool", sessionPool));
            activity?.SetTag("mqr.session.id", sessionLockInfo.SessionId);
            // Only decrement free sessions after a successful lock acquisition
            mqrMetrics.FreeSessions.Add(-1);
            decremented = true;
            return pool[sessionLockInfo.SessionId];
        }
        catch (Exception ex)
        {
            sw.Stop();
            MqrTracing.HSessionLockWaitMs.Record(sw.Elapsed.TotalMilliseconds, new KeyValuePair<string, object?>("pool", sessionPool));
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
            {
                { "exception.type", ex.GetType().FullName },
                { "exception.message", ex.Message }
            }));
            if (decremented)
            {
                // Compensate metric on failure after decrement
                mqrMetrics.FreeSessions.Add(1);
            }
            throw;
        }
    }

    public async Task ReleaseSessionLockAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        await using var dbcontext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        logger.LogInformation("ReleaseSessionAsync {SessionId}", sessionId);
        await dbcontext.ReleaseSessionAndUpdateRawOutputAsync(sessionId, string.Empty, cancellationToken);
        // Only increment free sessions after successfully releasing the lock in DB
        mqrMetrics.FreeSessions.Add(1);
    }
    
    public async Task<List<SessionPool>> GetSessionPools(CancellationToken cancellationToken = default)
    {
        return _sessionPools.Values.ToList();
    }

    public async Task SetupSessionPoolsAsync(List<InstructionSetProvider.SessionPoolInfo> instructionSets, CancellationToken cancellationToken = default)
    {
        foreach (var item in instructionSets)
        {
            var sessionPoolId = item.PoolId;
            if (_sessionPools.TryAdd(
                    sessionPoolId,
                    CreateSessionPool(sessionPoolId)))
            {
                mqrMetrics.TotalSessions.Add(1);
            }
            else
            {
                throw new InvalidOperationException($"Failed to create session pool {sessionPoolId}");
            }
        }
    }
    
    public async Task ShutdownSessionsAsync(string reason, CancellationToken cancellationToken = default)
    {
        try
        {
            var totalSessionCount = 0;
            foreach (var pool in _sessionPools.Values)
            {
                logger.LogInformation("ShutdownSessionsAsync {PoolId} {Reason}", pool.PoolId, reason);

                var sessions = pool.Sessions.ToList();
                totalSessionCount += sessions.Count;

                foreach (var session in sessions)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await session.TerminateSessionAsync(reason, cancellationToken);
                }
            }

            if (totalSessionCount > 0)
            {
                mqrMetrics.TotalSessions.Add(-totalSessionCount);
            }
        }
        finally
        {
            _sessionPools.Clear();
        }
    }

    public async Task<SessionInstance> GetSessionBySessionIdAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        return _sessionPools.Values.SelectMany(s => s.Sessions)
                   .FirstOrDefault(s => s.SessionId == sessionId)
               ?? throw new KeyNotFoundException("Session not found: " + sessionId);
    }

    public async Task<SessionInstance> GetSessionByTopsNameAsync(string topsName, CancellationToken cancellationToken = default)
    {
        return _sessionPools.Values.SelectMany(s => s.Sessions)
                   .FirstOrDefault(s => s.TopsName == topsName)
               ?? throw new KeyNotFoundException("Session not found: " + topsName);
    }

    private readonly Lock TimeShiftLock = new();

    private TimeSpan _serverToMfTimeDiff;

    private const double TOLERANCE = 0.99999;
    public void UpdateTimeShift(TimeSpan diff)
    {
        lock (TimeShiftLock)
        {
            if (Math.Abs(diff.TotalSeconds - _serverToMfTimeDiff.TotalSeconds) > TOLERANCE)
            {
                _serverToMfTimeDiff = diff;
            }
        }
    }
 
    private SessionPool CreateSessionPool(string poolId)
    {
        if (string.IsNullOrWhiteSpace(poolId)) throw new ArgumentException("Pool id must be provided", nameof(poolId));
        var sessionLogger = loggerFactory.CreateLogger($"{nameof(SessionPool)}_{poolId}");
        logger.LogDebug("Creating SessionPool for {PoolId}", poolId);
        var pool = new SessionPool(sessionLogger) { PoolId = poolId };
        return pool;
    }
}