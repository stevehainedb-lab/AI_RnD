using MQR.Services.Instructions.Models.Shared;
using MQR.Services.Observability;
using Open3270.Interfaces;

namespace MQR.Services.MainframeAction.Sessions.Abstractions;

public interface IScreenInputWriter
{
    ITnEmulator Emulator { get; }
    Task WriteInputsAsync(List<ScreenInput> inputs, Func<string, string>? getValueMethod, IMainframeIoLogger? mainframeIoLogger = null, CancellationToken cancellationToken = default);
    Task SetFieldAsync(ScreenPosition input, string valueToWrite, IMainframeIoLogger? mainframeIoLogger = null, CancellationToken cancellationToken = default);
}