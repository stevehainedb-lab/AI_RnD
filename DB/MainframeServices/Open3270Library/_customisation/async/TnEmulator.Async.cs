using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Open3270.TN3270;

namespace Open3270;

/// <summary>
/// Async wrappers for TnEmulator. Prefer true async where supported by lower layers; fall back to Task.Run for CPU-bound or legacy sync operations.
/// </summary>
public partial class TnEmulator
{
	// Utility waiters
    public async Task WaitTillKeyboardUnlockedAsync(int timeoutms, CancellationToken cancellationToken = default)
    {
        var dttm = DateTime.Now.AddMilliseconds(timeoutms);
        while (KeyboardLocked != 0 && DateTime.Now < dttm)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(10, cancellationToken).ConfigureAwait(false);
        }
    }
    // Connection and lifecycle
    public Task ConnectAsync(CancellationToken cancellationToken = default)
        => ConnectAsync(Config.HostName, Config.HostPort, Config.HostLu, cancellationToken);

    public async Task ConnectAsync(string localIp, string host, int port, CancellationToken cancellationToken = default)
    {
        LocalIp = localIp;
        await ConnectAsync(host, port, string.Empty, cancellationToken).ConfigureAwait(false);
    }

    public async Task ConnectAsync(string host, int port, string lu, CancellationToken cancellationToken = default)
    {
        // Mirror logic from sync Connect but use native async in Tn3270Api
        if (_currentConnection != null)
        {
            _currentConnection.Disconnect();
            _currentConnection.CursorLocationChanged -= currentConnection_CursorLocationChanged;
        }

        try
        {
            _semaphore.Reset();

            _currentConnection = null;
            _currentConnection = new Tn3270Api();
            _currentConnection.Debug = Debug;
            _currentConnection.RunScriptRequested += currentConnection_RunScriptEvent;
            _currentConnection.CursorLocationChanged += currentConnection_CursorLocationChanged;
            _currentConnection.Disconnected += _apiOnDisconnectDelegate;

            _apiOnDisconnectDelegate = currentConnection_OnDisconnect;

            if (Audit != null)
            {
                Audit.WriteLine("Open3270 emulator version " + System.Reflection.Assembly.GetAssembly(typeof(TnEmulator))?.GetName().Version);
                if (Debug)
                {
                    Config.Dump(Audit);
                    Audit.WriteLine("Connect to host \"" + host + "\"");
                    Audit.WriteLine("           port \"" + port + "\"");
                    Audit.WriteLine("           LU   \"" + lu + "\"");
                    Audit.WriteLine("     Local IP   \"" + LocalIp + "\"");
                }
            }

            _currentConnection.UseSsl = UseSsl;

            if (!string.IsNullOrEmpty(LocalIp))
                await _currentConnection.ConnectAsync(Audit, LocalIp, host, port, Config, cancellationToken).ConfigureAwait(false);
            else
                await _currentConnection.ConnectAsync(Audit, host, port, lu, Config, cancellationToken).ConfigureAwait(false);

            DisposeOfCurrentScreenXml();
        }
        catch
        {
            _currentConnection = null;
            throw;
        }
        
        await RefreshAsync(true, 10000, cancellationToken).ConfigureAwait(false);
        if (Audit != null && Debug) Audit.WriteLine("Debug::Connected");
    }

    public Task CloseAsync(CancellationToken cancellationToken = default)
        => Task.Run(() => { cancellationToken.ThrowIfCancellationRequested(); Close(); }, cancellationToken);

    // Screen/state refresh
    public Task RefreshAsync(CancellationToken cancellationToken = default)
        => Task.Run(() => { cancellationToken.ThrowIfCancellationRequested(); Refresh(); }, cancellationToken);

    public async Task<bool> RefreshAsync(bool waitForValidScreen, int timeoutMs, CancellationToken cancellationToken = default)
    {
        // True-async version of Refresh's waiting behavior. We still delegate blocking semaphore waits to a background thread.
        var start = DateTime.Now.Ticks / (10 * 1000);
        var end = start + timeoutMs;

        if (_currentConnection == null) throw new TnHostException("TNEmulator is not connected", "There is no currently open TN3270 connection", null);
        if (Audit != null && Debug) Audit.WriteLine("RefreshAsync::Refresh(" + waitForValidScreen + ", " + timeoutMs + "). FastScreenMode=" + Config.FastScreenMode);

        do
        {
            if (waitForValidScreen)
            {
                int timeout;
                do
                {
                    timeout = (int)(end - DateTime.Now.Ticks / 10000);
                    if (timeout > 0)
                    {
                        if (Audit != null && Debug) Audit.WriteLine("RefreshAsync::Acquire(" + timeout + " milliseconds). unsafe Count is currently " + _semaphore.Count);

                        var acquireResult = await Task.Run(() => _semaphore.Acquire(Math.Min(timeout, 1000)), cancellationToken).ConfigureAwait(false);

                        if (!IsConnected) throw new TnHostException("The TN3270 connection was lost", _currentConnection.DisconnectReason, null);

                        if (acquireResult)
                        {
                            if (Audit != null && Debug) Audit.WriteLine("RefreshAsync::return true (acquired)");
                            return true;
                        }
                    }
                } while (timeout > 0);

                if (Audit != null && Debug) Audit.WriteLine("RefreshAsync::Timeout or acquire failed.");
            }

            if (Config.FastScreenMode || KeyboardLocked == 0)
            {
                DisposeOfCurrentScreenXml();
                if (Audit != null && Debug) Audit.WriteLine("RefreshAsync::Keyboard unlocked or fast mode - return true");
                return true;
            }

            await Task.Delay(10, cancellationToken).ConfigureAwait(false);
        } while (DateTime.Now.Ticks / 10000 < end);

        if (Audit != null) Audit.WriteLine("RefreshAsync::Timed out waiting for a valid screen. Timeout was " + timeoutMs);

        if (!Config.FastScreenMode && Config.ThrowExceptionOnLockedScreen && KeyboardLocked != 0)
            throw new ApplicationException(
                "Timeout waiting for new screen with keyboard inhibit false - screen present with keyboard inhibit. Turn off the configuration option 'ThrowExceptionOnLockedScreen' to turn off this exception. Timeout was " +
                timeoutMs + ".");

        return Config.FastScreenMode || KeyboardLocked == 0;
    }

    // Host settle
    public async Task<bool> WaitForHostSettleAsync(int screenCheckInterval, int finalTimeout, CancellationToken cancellationToken = default)
    {
        // Mirror the synchronous WaitForHostSettle but use RefreshAsync in a loop.
        // Returns true when screen stops changing within polling windows, false if final timeout reached.
        var elapsed = 0;
        while (!await RefreshAsync(true, screenCheckInterval, cancellationToken).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (elapsed > finalTimeout)
            {
                return false;
            }
            elapsed += screenCheckInterval;
        }
        return true;
    }

    // Input
    public Task SetCursorAsync(int inputXPos, int inputYPos, CancellationToken cancellationToken = default)
        => Task.Run(() => { cancellationToken.ThrowIfCancellationRequested(); SetCursor(inputXPos, inputYPos); }, cancellationToken);

    public Task SetFieldAsync(int inputFieldNumber, string valueToWrite, CancellationToken cancellationToken = default)
        => Task.Run(() => { cancellationToken.ThrowIfCancellationRequested(); SetField(inputFieldNumber, valueToWrite); }, cancellationToken);

    public Task<bool> SendTextAsync(string valueToWrite, CancellationToken cancellationToken = default)
        => Task.Run(() => { cancellationToken.ThrowIfCancellationRequested(); return SendText(valueToWrite); }, cancellationToken);

    public Task<bool> SendKeyAsync(bool waitForScreenToUpdate, TnKey keyCommand, int timeoutMs, CancellationToken cancellationToken = default)
        => Task.Run(() => { cancellationToken.ThrowIfCancellationRequested(); return SendKey(waitForScreenToUpdate, keyCommand, timeoutMs); }, cancellationToken);

    // Waiting and reading
    public async Task<bool> WaitForTextAsync(int x, int y, string text, int timeoutMs, CancellationToken cancellationToken = default)
    {
        if (_currentConnection == null) throw new TnHostException("TNEmulator is not connected", "There is no currently open TN3270 connection", null);
        var start = DateTime.Now.Ticks;
        if (Config.AlwaysRefreshWhenWaiting)
            lock (this)
            {
                DisposeOfCurrentScreenXml();
            }

        do
        {
            if (CurrentScreen != null)
            {
                var screenText = CurrentScreen.GetText(x, y, text.Length);
                if (screenText == text)
                {
                    if (Audit != null) Audit.WriteLine("WaitForText('" + text + "') Found!");
                    return true;
                }
            }

            if (timeoutMs == 0)
            {
                if (Audit != null) Audit.WriteLine("WaitForText('" + text + "') Not found");
                return false;
            }

            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            if (Config.AlwaysRefreshWhenWaiting)
                lock (this)
                {
                    DisposeOfCurrentScreenXml();
                }

            await RefreshAsync(true, 1000, cancellationToken).ConfigureAwait(false);
        } while ((DateTime.Now.Ticks - start) / 10000 < timeoutMs);

        if (Audit != null) Audit.WriteLine("WaitForText('" + text + "') Timed out");
        return false;
    }

    public async Task<StringPosition> WaitForTextOnScreen2Async(int timeoutMs, string[] text, CancellationToken cancellationToken = default)
    {
        var idx = await WaitForTextOnScreenAsync(timeoutMs, text, cancellationToken).ConfigureAwait(false);
        if (idx != -1)
            return CurrentScreen.LookForTextStrings2(text);
        return null;
    }

    public async Task<int> WaitForTextOnScreenAsync(int timeoutMs, string[] text, CancellationToken cancellationToken = default)
    {
        if (_currentConnection == null) throw new TnHostException("TNEmulator is not connected", "There is no currently open TN3270 connection", null);
        var start = DateTime.Now.Ticks;
        if (Config.AlwaysRefreshWhenWaiting)
            lock (this)
            {
                DisposeOfCurrentScreenXml();
            }
        do
        {
            lock (this)
            {
                if (CurrentScreen != null)
                {
                    var index = CurrentScreen.LookForTextStrings(text);
                    if (index != -1)
                    {
                        if (Audit != null) Audit.WriteLine("WaitForText('" + text[index] + "') Found!");
                        return index;
                    }
                }
            }

            if (timeoutMs == 0)
            {
                if (Audit != null) Audit.WriteLine("WaitForTextOnScreen('" + string.Join(",", text) + "') Not found");
                return -1;
            }

            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            if (Config.AlwaysRefreshWhenWaiting)
                lock (this)
                {
                    DisposeOfCurrentScreenXml();
                }

            await RefreshAsync(true, 1000, cancellationToken).ConfigureAwait(false);
        } while ((DateTime.Now.Ticks - start) / 10000 < timeoutMs);

        if (Audit != null) Audit.WriteLine("WaitForTextOnScreen('" + string.Join(",", text) + "') Timed out");
        return -1;
    }
	
    public async Task<bool> WaitForRegexAsync(Func<string> getScreenData, string regExPattern, RegexOptions regExOptions, int timeoutMs, CancellationToken cancellationToken = default)
    {
        var regex = new Regex(regExPattern, regExOptions);
        if (_currentConnection == null) throw new TnHostException("TNEmulator is not connected", "There is no currently open TN3270 connection", null);
        var start = DateTime.Now.Ticks;
        if (Config.AlwaysRefreshWhenWaiting)
            lock (this)
            {
                DisposeOfCurrentScreenXml();
            }
        do
        {
            if (CurrentScreen != null)
            {
                var screenText = getScreenData();
                if (regex.IsMatch(screenText))
                {
                    Audit?.WriteLine($"WaitForRegex ['{regExPattern}'] Found!");
                    return true;
                }
            }

            if (timeoutMs == 0)
            {
                Audit?.WriteLine($"WaitForRegex [('{regExPattern}'] Not found");
                return false;
            }

            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            if (Config.AlwaysRefreshWhenWaiting)
                lock (this)
                {
                    DisposeOfCurrentScreenXml();
                }

            await RefreshAsync(true, 1000, cancellationToken).ConfigureAwait(false);
        } while ((DateTime.Now.Ticks - start) / 10000 < timeoutMs);

        Audit?.WriteLine($"WaitForRegex('{regExPattern}') Timed out");
        return false;
    }
}
