using MQR.Services.Instructions.Models.Shared;
using MQR.Services.MainframeAction.Sessions.Abstractions;
using MQR.Services.Observability;
using Open3270.Interfaces;
using System.Diagnostics;

namespace MQR.Services.MainframeAction.Sessions.Services;

internal sealed class ScreenWaiter : IScreenWaiter
{
    public ITnEmulator Emulator { get; }
    private readonly IScreenDataExtractor _dataExtractor;

    public ScreenWaiter(ITnEmulator emulator, IScreenDataExtractor dataExtractor)
    {
        Emulator = emulator;
        _dataExtractor = dataExtractor;
    }

    public async Task<bool> WaitForScreensAsync(List<ScreenIdentificationMark>? identificationMarkers, IMainframeIoLogger? mainframeIoLogger, CancellationToken cancellationToken = default)
    {
        using var activity = MqrTracing.ActivitySource.StartActivity("Screen.WaitForScreens", ActivityKind.Internal);
        cancellationToken.ThrowIfCancellationRequested();
        if (identificationMarkers == null || identificationMarkers.Count == 0)
            return true; // nothing to wait for

        activity?.SetTag("mqr.screen.marker.count", identificationMarkers.Count);

        // Return true if any of the identification markers match within their own timeout
        foreach (var marker in identificationMarkers)
        {
            if (await WaitForScreenAsync(marker, mainframeIoLogger, cancellationToken).ConfigureAwait(false))
                return true;
        }
        return false;
    }

    public async Task<bool> WaitForScreenAsync(ScreenIdentificationMark identificationMarker, IMainframeIoLogger? mainframeIoLogger, CancellationToken cancellationToken = default)
    {
        using var activity = MqrTracing.ActivitySource.StartActivity("Screen.WaitForScreen", ActivityKind.Internal);
        activity?.SetTag("mqr.screen.regex", identificationMarker.RegExPattern);
        activity?.SetTag("mqr.screen.area", identificationMarker.ScreenArea?.ToString());
        cancellationToken.ThrowIfCancellationRequested();
        // Honor per-marker timeout (WaitPeriod) from instruction set
        var timeout = identificationMarker.WaitPeriod;
        var sw = Stopwatch.StartNew();
        try
        {
            var ok = await _dataExtractor.WaitForTextAsync(identificationMarker.ScreenArea, identificationMarker.RegExPattern, timeout, cancellationToken);
            activity?.SetTag("mqr.screen.wait.ok", ok);
            return ok;
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
            sw.Stop();
            MqrTracing.HScreenWaitMs.Record(sw.Elapsed.TotalMilliseconds, new KeyValuePair<string, object?>("phase", "screen-wait"));
        }
    }
}