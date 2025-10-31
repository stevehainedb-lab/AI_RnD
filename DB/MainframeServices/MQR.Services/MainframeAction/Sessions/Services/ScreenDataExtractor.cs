using System;
using System.Data;
using System.Linq;
using MQR.Services.Instructions.Models.Shared;
using MQR.Services.MainframeAction.Sessions.Abstractions;
using MQR.Services.Observability;
using Open3270.Interfaces;

namespace MQR.Services.MainframeAction.Sessions.Services;

internal sealed class ScreenDataExtractor(ITnEmulator emulator) : IScreenDataExtractor
{
    public ITnEmulator Emulator { get; } = emulator;

    public async Task<List<(string Key, string Value)>> ExtractCapturePointsAsync(List<ScreenCaptureDataPoint> points, IMainframeIoLogger? mainframeIoLogger, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var list = new List<(string Key, string Value)>(points.Count);
        foreach (var p in points)
        {
            list.Add(await ExtractCapturePointAsync(p, mainframeIoLogger, cancellationToken).ConfigureAwait(false));
        }
        mainframeIoLogger?.LogWrapped(list.Select(kv => $"Captured {kv.Key} = {kv.Value}"));
        return list;
    }

    public Task<(string Key, string Value)> ExtractCapturePointAsync(ScreenCaptureDataPoint point, IMainframeIoLogger? mainframeIoLogger, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Use emulator's screen API directly (no extensions)
        var capturedValue = Emulator.CurrentScreen.GetText(point.ScreenArea, point.RegExPattern);
        if (!point.ExceptIfNotFound || !string.IsNullOrWhiteSpace(capturedValue))
            return Task.FromResult((point.Identifier, capturedValue));

        mainframeIoLogger?.LogImportantLine($"Could not find any data for {point.Identifier}");
        throw new InvalidExpressionException($"Could not find any data for {point.Identifier} Text Searched was: " + Emulator.CurrentScreen.GetText(point.ScreenArea));
    }

    public Task<string> GetVisibleScreenAsync(IMainframeIoLogger? mainframeIoLogger = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Dump the current screen contents and return text
        var text = Emulator.CurrentScreen.Dump();
        if (mainframeIoLogger != null)
        {
            mainframeIoLogger.LogWrapped([text]);
        }
        return Task.FromResult(text);
    }

    public Task<bool> WaitForTextAsync(ScreenArea area, RegExPattern regex, TimeSpan? timeout, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var timeoutMs = timeout.HasValue ? (int)timeout.Value.TotalMilliseconds : 0;
        // poll using emulator's regex wait helper over the provided area
        bool result = Emulator.WaitForRegex(() => Emulator.CurrentScreen.GetText(area), regex.Pattern, regex.RegExOptions, timeoutMs);
        return Task.FromResult(result);
    }
}