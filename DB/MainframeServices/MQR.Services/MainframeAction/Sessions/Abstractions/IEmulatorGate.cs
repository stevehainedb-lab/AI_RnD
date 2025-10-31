using Open3270.Interfaces;

namespace MQR.Services.MainframeAction.Sessions.Abstractions;

/// <summary>
/// Serializes access to a single <see cref="ITnEmulator"/> allowing true async awaits inside critical sections.
/// </summary>
public interface IEmulatorGate
{
    Task<IDisposable> AcquireAsync(CancellationToken cancellationToken = default);
}