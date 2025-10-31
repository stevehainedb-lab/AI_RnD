using MQR.Services.Instructions.Models.Shared;
using MQR.Services.Observability;
using Open3270.Interfaces;

namespace MQR.Services.MainframeAction.Sessions.Abstractions;

public interface IScreenWaiter
{
    ITnEmulator Emulator { get; }
    Task<bool> WaitForScreensAsync(List<ScreenIdentificationMark>? identificationMarkers, IMainframeIoLogger? mainframeIoLogger, CancellationToken cancellationToken = default);
    Task<bool> WaitForScreenAsync(ScreenIdentificationMark identificationMarker, IMainframeIoLogger? mainframeIoLogger, CancellationToken cancellationToken = default);
}