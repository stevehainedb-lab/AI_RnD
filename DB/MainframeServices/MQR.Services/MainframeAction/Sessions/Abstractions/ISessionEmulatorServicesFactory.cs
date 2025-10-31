using MQR.Services.MainframeAction.Sessions.Models;
using Open3270.Interfaces;

namespace MQR.Services.MainframeAction.Sessions.Abstractions;

public interface ISessionEmulatorServicesFactory
{
    SessionEmulatorServices Create(ITnEmulator emulator);
}
