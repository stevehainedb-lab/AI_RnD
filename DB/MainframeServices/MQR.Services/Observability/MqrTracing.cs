using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace MQR.Services.Observability;

/// <summary>
/// Shared OpenTelemetry tracing and metrics instrumentation for Mainframe operations.
/// </summary>
public static class MqrTracing
{
    // ActivitySource for tracing spans in MainframeAction
    public static readonly ActivitySource ActivitySource = new("MQR.MainframeAction");

    // Use the same meter name as metrics to keep a single logical instrumentation library
    private static readonly Meter Meter = new(MqrMetrics.MeterName);

    // Histograms (milliseconds) for key operations
    public static readonly Histogram<double> HSessionLockWaitMs =
        Meter.CreateHistogram<double>(
            name: "mqr.session.lock.wait.ms",
            unit: "ms",
            description: "Time spent waiting to acquire a mainframe session lock");

    public static readonly Histogram<double> HQueryDurationMs =
        Meter.CreateHistogram<double>(
            name: "mqr.query.total.ms",
            unit: "ms",
            description: "Total duration of a query executed on a mainframe session");

    public static readonly Histogram<double> HSendKeyLatencyMs =
        Meter.CreateHistogram<double>(
            name: "mqr.navigation.sendkey.ms",
            unit: "ms",
            description: "Latency for sending a key command to the mainframe and associated waiting");

    public static readonly Histogram<double> HScreenWaitMs =
        Meter.CreateHistogram<double>(
            name: "mqr.screen.wait.ms",
            unit: "ms",
            description: "Time spent waiting for screen refreshes or specific screens");
}