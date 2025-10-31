using System.Diagnostics;
using MQR.DataAccess.Entities;
using MQR.Services.Instructions.Models.Shared;
using MQR.Services.MainframeAction.Sessions.Abstractions;
using MQR.Services.Observability;
using System.Diagnostics;
using Open3270.Interfaces;

namespace MQR.Services.MainframeAction.Sessions.Services;

internal sealed class NavigationExecutor(ITnEmulator emulator) : INavigationExecutor
{
    public ITnEmulator Emulator { get; } = emulator;

    public async Task<TransactionData?> SendKeyCommandAsync(
        KeyCommand command,
        TimeSpan? timeout,
        string task,
        int? screenRefreshes,
        IMainframeIoLogger? log,
        CancellationToken cancellationToken = default)
    {
        using var activity = MqrTracing.ActivitySource.StartActivity("Navigation.SendKey", ActivityKind.Internal);
        activity?.SetTag("mqr.nav.key", command.ToString());
        activity?.SetTag("mqr.task", task);
        activity?.SetTag("mqr.nav.screenRefreshes", screenRefreshes);
        activity?.SetTag("mqr.nav.timeout.ms", timeout?.TotalMilliseconds);
        cancellationToken.ThrowIfCancellationRequested();
        var opSw = Stopwatch.StartNew();

        try
        {
            // Fast path: no explicit refresh count and no explicit timeout provided
            if (!screenRefreshes.HasValue && !timeout.HasValue)
            {
                var defaultTimeout = TimeSpan.FromSeconds(5);
                LogSendKey(command, log);
                var ok = Emulator.SendKey(true, command.ToTnKey(), (int)defaultTimeout.TotalMilliseconds);
                if (!ok)
                    throw new InvalidOperationException($"Key command failed: {command} in task '{task}'.");

                // Capture current screen for diagnostics
                _ = GetVisibleScreenSafe(log);
                return new TransactionData();
            }

            var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(5);
            var sw = Stopwatch.StartNew();
            log?.LogImportantLine($"SEND {command} then WAIT for {screenRefreshes} screen refresh(es) (timeout {effectiveTimeout}).");

            // Baseline screen
            var beforeScreen = GetVisibleScreenSafe(log);

            // Send key with timeout support
            LogSendKey(command, log);
            var sendOk = Emulator.SendKey(true, command.ToTnKey(), (int)effectiveTimeout.TotalMilliseconds);
            if (!sendOk)
                throw new InvalidOperationException($"Key command failed: {command} in task '{task}'.");

            var result = new TransactionData();

            // If no refresh expectation, just refresh/snapshot and return
            if ((screenRefreshes ?? 0) <= 0)
            {
                RefreshScreen(null, log);
                _ = GetVisibleScreenSafe(log);
                return result;
            }

            var refreshesSeen = 0;
            var waitAttempt = 0;
            var deadline = effectiveTimeout; // relative timeout
            var afterScreen = GetVisibleScreenSafe();

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!string.Equals(beforeScreen, afterScreen, StringComparison.Ordinal))
                {
                    refreshesSeen++;
                    log?.LogImportantLine($"SCREEN CHANGED ({refreshesSeen}/{screenRefreshes}). Elapsed: {sw.Elapsed}.");
                    beforeScreen = afterScreen;
                    if (refreshesSeen >= screenRefreshes)
                    {
                        log?.LogCurrentScreen(afterScreen);
                        return result;
                    }
                }

                if (sw.Elapsed >= deadline)
                {
                    var errorText = $"Screen did not refresh {screenRefreshes} time(s). Task: '{task}'. Elapsed: {sw.Elapsed}, Timeout: {effectiveTimeout}.";
                    log?.LogWrapped([errorText]);
                    throw new TimeoutException(errorText);
                }

                var delayMs = (int)Math.Min(100 * Math.Pow(1.3, waitAttempt), 5000);
                MqrTracing.HScreenWaitMs.Record(delayMs, new KeyValuePair<string, object?>("phase", "nav-refresh-wait"));
                log?.LogImportantLine($"Waiting {delayMs} ms before refresh (attempt {waitAttempt + 1}). Elapsed: {sw.Elapsed}.");
                await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);

                RefreshScreen(null, log);
                afterScreen = GetVisibleScreenSafe();
                waitAttempt++;
            }
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
            opSw.Stop();
            MqrTracing.HSendKeyLatencyMs.Record(opSw.Elapsed.TotalMilliseconds, new KeyValuePair<string, object?>("key", command.ToString())) ;
        }
    }

    public async Task<TransactionData?> DoNavigationAsync(
        NavigationAction navigationAction,
        string task,
        IMainframeIoLogger? log,
        CancellationToken cancellationToken = default)
    {
        using var activity = MqrTracing.ActivitySource.StartActivity("Navigation.DoNavigation", ActivityKind.Internal);
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(task)) task = "Navigation";
        var key = navigationAction.NavigationKey;
        var timeout = navigationAction.NavigationTimeout;
        activity?.SetTag("mqr.nav.key", key.ToString());
        activity?.SetTag("mqr.task", task);
        activity?.SetTag("mqr.nav.timeout.ms", timeout?.TotalMilliseconds);
        activity?.SetTag("mqr.nav.refreshes", navigationAction.ScreenRefreshes);
        activity?.SetTag("mqr.nav.wait.ms", navigationAction.NavigationWait?.TotalMilliseconds);
        try
        {
            // Branch 1: Wait for specific number of screen refreshes
            if (navigationAction.ScreenRefreshes is > 0)
            {
                var refreshes = navigationAction.ScreenRefreshes.Value;
                log?.LogImportantLine($"[{task}] Sending {key} and waiting for {refreshes} screen refresh(es).");

                var tx = await SendKeyCommandAsync(
                    key,
                    timeout,
                    task,
                    refreshes,
                    log,
                    cancellationToken).ConfigureAwait(false);

                RefreshScreen(null, log);
                _ = GetVisibleScreenSafe(log);
                return tx;
            }

            // Branch 2: Wait for a fixed duration after sending
            if (navigationAction.NavigationWait is { } wait && wait > TimeSpan.Zero)
            {
                log?.LogImportantLine($"[{task}] Sending {key} then waiting {wait.TotalMilliseconds} ms.");

                var tx = await SendKeyCommandAsync(
                    key,
                    timeout,
                    task,
                    null,
                    log,
                    cancellationToken).ConfigureAwait(false);

                await Task.Delay(wait, cancellationToken).ConfigureAwait(false);
                MqrTracing.HScreenWaitMs.Record(wait.TotalMilliseconds, new KeyValuePair<string, object?>("phase", "nav-fixed-wait"));
                RefreshScreen(null, log);
                _ = GetVisibleScreenSafe(log);
                return tx;
            }

            // Branch 3: Default send (no explicit wait or refresh count)
            log?.LogImportantLine($"{task} Sending {key} with no additional wait.");
            var result = await SendKeyCommandAsync(
                key,
                timeout,
                task,
                screenRefreshes: 0,
                log,
                cancellationToken).ConfigureAwait(false);

            RefreshScreen(null, log);
            _ = GetVisibleScreenSafe(log);
            return result;
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
    }

    private void RefreshScreen(TimeSpan? timeout = default, IMainframeIoLogger? log = default)
    {
        log?.LogImportantLine("SCREEN REFRESH");
        if (!timeout.HasValue)
            Emulator.Refresh();
        else
            Emulator.Refresh(true, (int)timeout.Value.TotalMilliseconds);
    }

    private string GetVisibleScreenSafe(IMainframeIoLogger? log = default)
    {
        try
        {
            if (Emulator is { IsConnected: false } && Emulator.CurrentScreen?.Dump() is { } dump)
            {
                log?.LogCurrentScreen(dump);
                return dump;
            }
            return "-- NOT CONNECTED --";
        }
        catch (Exception ex)
        {
            var msg = $"-- EXCEPTION WHILE TRYING TO ACCESS SCREEN -- \n {ex}";
            log?.LogCurrentScreen(msg);
            return msg;
        }
    }

    private static void LogSendKey(KeyCommand keyCommand, IMainframeIoLogger? log)
    {
        log?.LogImportantLine("SENDING NAVIGATION KEY : " + keyCommand);
    }
}