#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Open3270.TN3270;

public partial class Tn3270Api
{
	public Task ConnectAsync(IAudit audit, string localIp, string host, int port, ConnectionConfig config, CancellationToken cancellationToken = default)
    {
        _sourceIp = localIp;
        return ConnectAsync(audit, host, port, string.Empty, config, cancellationToken);
    }

    public async Task ConnectAsync(IAudit audit, string host, int port, string lu, ConnectionConfig config, CancellationToken cancellationToken = default)
    {
        if (_tn != null) _tn.CursorLocationChanged -= tn_CursorLocationChanged;

        _tn = new Telnet(this, audit, config);
        
        _tn.Trace.optionTraceAnsi = _debug;
        _tn.Trace.optionTraceDS = _debug;
        _tn.Trace.optionTraceDSN = _debug;
        _tn.Trace.optionTraceEvent = _debug;
        _tn.Trace.optionTraceNetworkData = _debug;

        _tn.telnetDataEventOccurred += tn_DataEventReceived;
        _tn.CursorLocationChanged += tn_CursorLocationChanged;

        if (string.IsNullOrEmpty(lu))
        {
            _tn.Lus = null;
        }
        else
        {
            _tn.Lus = new System.Collections.Generic.List<string> { lu };
        }

        try
        {
            if (!_tn.IsSocketConnected)
            {
                if (!string.IsNullOrEmpty(_sourceIp))
                    await _tn.ConnectAsync(this, host, port, _sourceIp, cancellationToken).ConfigureAwait(false);
                else
                    await _tn.ConnectAsync(this, host, port, cancellationToken).ConfigureAwait(false);
            }

            if (!_tn.IsConnected)
            {
                var text = _tn.DisconnectReason;
                await _tn.DisconnectAsync(cancellationToken);
                _tn = null;
                throw new TnHostException("connect to " + host + " on port " + port + " failed", text, null);
            }
            
            if (config.KeepAlivePeriod is { TotalSeconds: > 0 })
            {
	            _tn.StartKeepAlive(config.KeepAlivePeriod.Value);
            }
            
            _tn.Trace.WriteLine("--connected");
        }
        catch (OperationCanceledException)
        {
            await _tn?.DisconnectAsync(cancellationToken)!;
            _tn = null;
            throw;
        }
    }

    public Task<bool> WaitForConnectAsync(int timeoutMs, CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (_tn == null)
        {
            tcs.SetResult(false);
            return tcs.Task;
        }

        if (_tn.IsConnected)
        {
            tcs.SetResult(true);
            return tcs.Task;
        }

        void OnConnected3270(object? sender, Connected3270EventArgs e) => tcs.TrySetResult(true);
        void OnConnectedLineMode(object? sender, EventArgs e) => tcs.TrySetResult(true);
        void OnPrimaryConnectionChanged(object? sender, PrimaryConnectionChangedArgs e)
        {
            if (!e.Success) tcs.TrySetResult(false);
        }

        _tn.Connected3270 += OnConnected3270;
        _tn.ConnectedLineMode += OnConnectedLineMode;
        _tn.PrimaryConnectionChanged += OnPrimaryConnectionChanged;

        var timeoutTask = timeoutMs > 0 ? Task.Delay(timeoutMs, CancellationToken.None) : Task.Delay(-1, CancellationToken.None);

        return WaitAsync();

        async Task<bool> WaitAsync()
        {
            CancellationTokenRegistration ctr = default;
            try
            {
                if (cancellationToken.CanBeCanceled)
                    ctr = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));

                var completed = await Task.WhenAny(tcs.Task, timeoutTask).ConfigureAwait(false);
                if (completed == timeoutTask)
                {
                    return false;
                }
                return await tcs.Task.ConfigureAwait(false);
            }
            finally
            {
                ctr.Dispose();
                if (_tn != null)
                {
                    _tn.Connected3270 -= OnConnected3270;
                    _tn.ConnectedLineMode -= OnConnectedLineMode;
                    _tn.PrimaryConnectionChanged -= OnPrimaryConnectionChanged;
                }
            }
        }
    }
}
