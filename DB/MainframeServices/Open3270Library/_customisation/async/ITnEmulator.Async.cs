using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Open3270.Interfaces;

/// <summary>
/// Async extension to ITnEmulator. This partial interface adds Task-based methods mirroring the synchronous API.
/// Existing synchronous members remain unchanged for backward compatibility.
/// </summary>
public partial interface ITnEmulator
{
    // Connection and lifecycle
    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task ConnectAsync(string localIp, string host, int port, CancellationToken cancellationToken = default);
    Task ConnectAsync(string host, int port, string lu, CancellationToken cancellationToken = default);
    Task CloseAsync(CancellationToken cancellationToken = default);

    // Screen/state refresh
    Task RefreshAsync(CancellationToken cancellationToken = default);
    Task<bool> RefreshAsync(bool waitForValidScreen, int timeoutMs, CancellationToken cancellationToken = default);

    // Input
    Task SetCursorAsync(int inputXPos, int inputYPos, CancellationToken cancellationToken = default);
    Task SetFieldAsync(int inputFieldNumber, string valueToWrite, CancellationToken cancellationToken = default);
    Task<bool> SendTextAsync(string valueToWrite, CancellationToken cancellationToken = default);
    Task<bool> SendKeyAsync(bool waitForScreenToUpdate, TnKey keyCommand, int timeoutMs, CancellationToken cancellationToken = default);

    // Waiting and reading
    Task<bool> WaitForTextAsync(int x, int y, string text, int timeoutMs, CancellationToken cancellationToken = default);
    Task<StringPosition> WaitForTextOnScreen2Async(int timeoutMs, string[] text, CancellationToken cancellationToken = default);
    Task<int> WaitForTextOnScreenAsync(int timeoutMs, string[] text, CancellationToken cancellationToken = default);
  
    Task<bool> WaitForRegexAsync(Func<string> getScreenData, string regExPattern, RegexOptions regExOptions, int timeoutMs, CancellationToken cancellationToken = default);

    // Host settle
    Task<bool> WaitForHostSettleAsync(int screenCheckInterval, int finalTimeout, CancellationToken cancellationToken = default);
}
