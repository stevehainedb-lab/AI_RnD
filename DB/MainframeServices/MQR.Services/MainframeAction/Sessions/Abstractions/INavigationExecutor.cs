using MQR.Services.Instructions.Models.Shared;
using MQR.Services.Observability;
using Open3270.Interfaces;
using MQR.DataAccess.Entities;

namespace MQR.Services.MainframeAction.Sessions.Abstractions;

public interface INavigationExecutor
{
    ITnEmulator Emulator { get; }
    Task<TransactionData?> SendKeyCommandAsync(KeyCommand command, TimeSpan? timeout, string task, int? screenRefreshes, IMainframeIoLogger? log, CancellationToken cancellationToken = default);
    Task<TransactionData?> DoNavigationAsync(NavigationAction navigationAction, string task, IMainframeIoLogger? log, CancellationToken cancellationToken = default);
}