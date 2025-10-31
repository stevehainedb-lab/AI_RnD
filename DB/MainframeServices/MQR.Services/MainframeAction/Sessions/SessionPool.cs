using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using MQR.DataAccess.Context;

namespace MQR.Services.MainframeAction.Sessions;

public class SessionPool(ILogger logger) : ConcurrentDictionary<string, SessionInstance>, IDisposable
{
    private readonly IDisposable? _loggerScope;
    private readonly string _poolId; 
    public required string PoolId
    {
        get => _poolId;
        init
        {
                
            _poolId = value ?? throw new ArgumentNullException(nameof(value));
            _loggerScope?.Dispose();
            _loggerScope = logger.BeginScope(value);
        }
    }
        
    public IEnumerable<SessionInstance> Sessions => Values;
        
    public void Dispose()
    {
        _loggerScope?.Dispose();
    }
    
}