using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Formats.Asn1;

namespace MQR.Services.Observability;

/// <summary>
/// Provides metrics instrumentation for the MQR application.
/// </summary>
public class MqrMetrics
{
    public const string MeterName = "MQR.WebAPI";
    
    private static readonly ConcurrentDictionary<string, int> SessionCounts = new();
    public UpDownCounter<int> TotalSessions { get; }
    public UpDownCounter<int> FreeSessions { get; }
    
    public Counter<int> UnhealthySessions { get; }
    public Counter<int> QueriesRun { get; }
    
    public ObservableGauge<int> SessionsGauge { get; set; }

    public void SetSessionCount(string instructionSet, int value)
    {
        SessionCounts[instructionSet.ToUpperInvariant()] = value;
    }
    
    public MqrMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        
        // UpDownCounter: Tracks currently active requests
        TotalSessions = meter.CreateUpDownCounter<int>(
            "mqr.sessions.total",
            unit: "{session}",
            description: "Number of active mainframe sessions");
        
        FreeSessions = meter.CreateUpDownCounter<int>(
            "mqr.sessions.free",
            unit: "{session}",
            description: "Number of mainframe sessions available for work");
        
        UnhealthySessions = meter.CreateCounter<int>(
            "mqr.sessions.unhealthy",
            unit: "{session}",
            description: "Number of unhealthy mainframe sessions");
        
        QueriesRun = meter.CreateCounter<int>(
            "mqr.sessions.queries.run",
            unit: "{query}",
            description: "Number of queries run against mainframe sessions");
        
        SessionsGauge = meter.CreateObservableGauge(
            "sessions.current", 
            ObserveSessions,
            unit: "{session}",
            description: "Number of active mainframe sessions");
    }


    private static IEnumerable<Measurement<int>> ObserveSessions()
    {
        foreach (var kvp in SessionCounts)
        {
            yield return new Measurement<int>(
                kvp.Value,
                new KeyValuePair<string, object?>("instructionSet", kvp.Key));
        }
    }
}
