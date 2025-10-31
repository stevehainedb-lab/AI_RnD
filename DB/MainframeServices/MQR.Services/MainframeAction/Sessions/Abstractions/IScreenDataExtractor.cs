using MQR.Services.Instructions.Models.Shared;
using MQR.Services.Observability;
using Open3270.Interfaces;

namespace MQR.Services.MainframeAction.Sessions.Abstractions;

public interface IScreenDataExtractor
{
    ITnEmulator Emulator { get; }
    Task<List<(string Key, string Value)>> ExtractCapturePointsAsync(List<ScreenCaptureDataPoint> points, IMainframeIoLogger? mainframeIoLogger, CancellationToken cancellationToken = default);
    Task<(string Key, string Value)> ExtractCapturePointAsync(ScreenCaptureDataPoint point, IMainframeIoLogger? mainframeIoLogger, CancellationToken cancellationToken = default);
    Task<string> GetVisibleScreenAsync(IMainframeIoLogger? mainframeIoLogger = null, CancellationToken cancellationToken = default);
    Task<bool> WaitForTextAsync(ScreenArea area, RegExPattern regex, TimeSpan? timeout, CancellationToken cancellationToken = default);
}