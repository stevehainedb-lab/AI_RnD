using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQR.Services.Model;
using MQR.Services.Observability;

namespace MQR.Services.MainframeAction.Sessions;

public class SessionScaler(
    ISessionProvider sessionProvider,
    IInstructionSetProvider instructionSetProvider,
    MqrMetrics metrics,
    IOptions<MqrConfig> config,
    ILogger<SessionScaler> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var sessionPoolRequirements = await instructionSetProvider.GetSessionPoolRequirements(stoppingToken);
        await sessionProvider.SetupSessionPoolsAsync(sessionPoolRequirements, stoppingToken);
        using var timer = new PeriodicTimer(config.Value.SessionScalerInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var pools = await sessionProvider.GetSessionPools(stoppingToken);
            foreach (var pool in pools)
            {
                stoppingToken.ThrowIfCancellationRequested();
                await CheckPool(pool, stoppingToken);
            }
        }
    }

    private async Task CheckPool(SessionPool pool, CancellationToken stoppingToken)
    {
        logger.LogInformation("Checking pool {PoolId} for session health", pool.PoolId);
        try
        {
            await CheckSessionHealth(pool, stoppingToken);
            await CreateSessionsInPoolsWithoutEnoughAllocated(pool, stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception in SessionScaler - CheckPool");
        }
    }

    private async Task CreateSessionsInPoolsWithoutEnoughAllocated(SessionPool pool, CancellationToken stoppingToken)
    {
        logger.LogInformation("CreateSessionsInPoolsWithoutEnoughAllocated pool {PoolId} for session health", pool.PoolId);
        try
        {
            var instructionSet = await instructionSetProvider.GetLogonInstructionSet(pool.PoolId, stoppingToken);
            
            var currentSessionCount = pool.Sessions.Count();
            if (currentSessionCount < instructionSet.InstanceSessionCount)
            {
                var needed = instructionSet.InstanceSessionCount - currentSessionCount;

                for (var count = 1; count <= needed; count++)
                {
                    stoppingToken.ThrowIfCancellationRequested();
                    await sessionProvider.CreateSessionInstance(instructionSet);
                }
            }

            metrics.SetSessionCount(instructionSet.Identifier, pool.Sessions.Count());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception in SessionScaler - CreateSessionsInPoolsWithoutEnoughAllocated");
        }
    }

    private async Task CheckSessionHealth(SessionPool pool, CancellationToken stoppingToken)
    {
        logger.LogInformation("CheckSessionHealth pool {PoolId} for session health", pool.PoolId);
        try
        {
            foreach (var session in pool.Sessions)
            {
                stoppingToken.ThrowIfCancellationRequested();
                
                var isHealthy = session.IsHealthy(out var reason);
                if (isHealthy)
                {
                    logger.LogTrace("Session Is Healthy {Session}", session);
                }
                else
                {
                    await session.TerminateSessionAsync($"Session was not healthy - {reason}!", stoppingToken);
                    metrics.UnhealthySessions.Add(1);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception in SessionScaler - CheckSessionHealth");
        }
    }
    
}