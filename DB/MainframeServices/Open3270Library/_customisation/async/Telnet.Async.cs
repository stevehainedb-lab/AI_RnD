using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Open3270.Library;

namespace Open3270.TN3270
{
	
	
	/// <summary>
	/// Async wrappers for Telnet. These expose Task-based APIs over the existing
	/// Begin/End and synchronous methods without changing core behavior.
	/// </summary>
	internal partial class Telnet
	{
		private PeriodicTask _keepAlive;

		// Start/stop keep-alive
		public void StartKeepAlive(TimeSpan interval, bool runImmediately = false)
		{
			_keepAlive?.Dispose();
			_keepAlive = new PeriodicTask(
				interval,
				callback: ct =>
				{
					if (ct.IsCancellationRequested) return ValueTask.FromCanceled(ct);
					if (!IsConnected) return ValueTask.CompletedTask;
					var buf = new[] { TelnetConstants.IAC, TelnetConstants.NOP };
					SendRawOutput(buf, buf.Length);
					
					Trace.trace_dsn("SENT NOP\n");
					
					return ValueTask.CompletedTask;
				},
				runImmediately: runImmediately
			);
		}

		public async Task StopKeepAliveAsync()
		{
			if (_keepAlive != null)
			{
				await _keepAlive.DisposeAsync().ConfigureAwait(false);
				_keepAlive = null;
			}
		}
		
		/// <summary>
		/// Connect to host asynchronously using async DNS resolution. Mirrors the sync Connect() initialization
		/// but replaces Dns.GetHostEntry with Dns.GetHostEntryAsync.
		/// </summary>
		public async Task ConnectAsync(
			object parameterObjectToSendCallbacks,
			string hostAddress,
			int hostPort,
			CancellationToken cancellationToken = default)
		{
			parentData = parameterObjectToSendCallbacks;
			address = hostAddress;
			port = hostPort;
			DisconnectReason = null;
			closeRequested = false;

			// Initialize terminal and controller, mirroring sync Connect
			if (Config.TermType == null)
				TermType = "IBM-3278-2";
			else
				TermType = Config.TermType;

			Controller.Initialize(-1);
			Controller.Reinitialize(-1);
			Keyboard.Initialize();
			Ansi.ansi_init();

			// Set up attributes and tracing same as sync
			Appres.Mono = false;
			Appres.M3279 = true;
			Appres.DebugTracing = true;
			if (!Appres.DebugTracing)
			{
				Appres.SetToggle(Appres.DsTrace, false);
				Appres.SetToggle(Appres.EventTrace, false);
			}
			Appres.SetToggle(Appres.DsTrace, true);

			if (Config.LogFile != null)
			{
				// Simulate a connect via log file replay (same as sync Connect)
				ParseLogFileOnly = true;
				logFileSemaphore = new MySemaphore(0, 9999);
				logClientData = new System.Collections.Queue();
				logFileProcessorThread_Quit = false;
				mainThread = Thread.CurrentThread;
				logFileProcessorThread = new Thread(LogFileProcessorThreadHandler);
				logFileProcessorThread.Start();
			}
			else
			{
				// Determine if address is raw IPv4 or hostname
				var ipaddress = false;
				var text = false;
				var count = 0;
				for (var i = 0; i < address.Length; i++)
					if (address[i] == '.') count++; else if (address[i] < '0' || address[i] > '9') text = true;
				if (count == 3 && !text) ipaddress = true;

				if (!ipaddress)
				{
					try
					{
						var hostEntry = await Dns.GetHostEntryAsync(address, cancellationToken).ConfigureAwait(false);
						var ipAddress = hostEntry.AddressList.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
						if (ipAddress == null)
						{
							throw new TnHostException("Unable to resolve an IPv4 address for host '" + address + "'", "No IPv4 address found in DNS results.", null);
						}
						remoteEndpoint = new IPEndPoint(ipAddress, port);
					}
					catch (SocketException se)
					{
						throw new TnHostException("Unable to resolve host '" + address + "'", se.Message, null);
					}
				}
				else
				{
					try
					{
						remoteEndpoint = new IPEndPoint(IPAddress.Parse(address), port);
					}
					catch (FormatException se)
					{
						throw new TnHostException("Invalid Host TCP/IP address '" + address + "'", se.Message, null);
					}
				}

				// Local endpoint (may be overridden by overload with sourceIP)
				localEndpoint = new IPEndPoint(IPAddress.Any, 0);
				DisconnectReason = null;

				try
				{
					// Create New Socket and begin async connect using existing callback
					socketBase = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
					socketBase.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
					callbackProc = ConnectCallback;
					connectionState = ConnectionState.Resolving;
					socketBase.Bind(localEndpoint);
					socketBase.BeginConnect(remoteEndpoint, callbackProc, socketBase);
				}
				catch (SocketException se)
				{
					throw new TnHostException("An error occured connecting to the host '" + address + "' on port " + port, se.Message, null);
				}
				catch (Exception eeeee)
				{
					Console.WriteLine("e=" + eeeee);
					throw;
				}
			}

			// Await the connection becoming established
			var connected = await WaitForConnectAsync(cancellationToken).ConfigureAwait(false);
			if (!connected)
			{
				throw new InvalidOperationException(DisconnectReason ?? "Failed to connect to host.");
			}
		}

		/// <summary>
		/// Connect to host with source IP asynchronously.
		/// </summary>
		public async Task ConnectAsync(
			object parameterObjectToSendCallbacks,
			string hostAddress,
			int hostPort,
			string sourceIP,
			CancellationToken cancellationToken = default)
		{
			this.sourceIP = sourceIP;
			parentData = parameterObjectToSendCallbacks;
			address = hostAddress;
			port = hostPort;
			DisconnectReason = null;
			closeRequested = false;

			// Initialize terminal and controller
			TermType = Config.TermType ?? "IBM-3278-2";
			Controller.Initialize(-1);
			Controller.Reinitialize(-1);
			Keyboard.Initialize();
			Ansi.ansi_init();
			Appres.Mono = false;
			Appres.M3279 = true;
			Appres.DebugTracing = true;
			if (!Appres.DebugTracing)
			{
				Appres.SetToggle(Appres.DsTrace, false);
				Appres.SetToggle(Appres.EventTrace, false);
			}
			Appres.SetToggle(Appres.DsTrace, true);

			if (Config.LogFile != null)
			{
				ParseLogFileOnly = true;
				logFileSemaphore = new MySemaphore(0, 9999);
				logClientData = new System.Collections.Queue();
				logFileProcessorThread_Quit = false;
				mainThread = Thread.CurrentThread;
				logFileProcessorThread = new Thread(LogFileProcessorThreadHandler);
				logFileProcessorThread.Start();
			}
			else
			{
				// Resolve remote endpoint (async DNS if needed)
				var ipaddress = false;
				var text = false;
				var count = 0;
				for (var i = 0; i < address.Length; i++)
					if (address[i] == '.') count++; else if (address[i] < '0' || address[i] > '9') text = true;
				if (count == 3 && !text) ipaddress = true;

				if (!ipaddress)
				{
					try
					{
						var hostEntry = await Dns.GetHostEntryAsync(address, cancellationToken).ConfigureAwait(false);
						var ipAddress = hostEntry.AddressList.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
						if (ipAddress == null)
						{
							throw new TnHostException("Unable to resolve an IPv4 address for host '" + address + "'", "No IPv4 address found in DNS results.", null);
						}
						remoteEndpoint = new IPEndPoint(ipAddress, port);
					}
					catch (SocketException se)
					{
						throw new TnHostException("Unable to resolve host '" + address + "'", se.Message, null);
					}
				}
				else
				{
					try
					{
						remoteEndpoint = new IPEndPoint(IPAddress.Parse(address), port);
					}
					catch (FormatException se)
					{
						throw new TnHostException("Invalid Host TCP/IP address '" + address + "'", se.Message, null);
					}
				}

				// Local endpoint based on sourceIP
				try
				{
					localEndpoint = new IPEndPoint(IPAddress.Parse(sourceIP), 0);
				}
				catch (FormatException se)
				{
					throw new TnHostException("Invalid Source TCP/IP address '" + sourceIP + "'", se.Message, null);
				}

				DisconnectReason = null;
				try
				{
					socketBase = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
					socketBase.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
					callbackProc = ConnectCallback;
					connectionState = ConnectionState.Resolving;
					socketBase.Bind(localEndpoint);
					socketBase.BeginConnect(remoteEndpoint, callbackProc, socketBase);
				}
				catch (SocketException se)
				{
					throw new TnHostException("An error occured connecting to the host '" + address + "' on port " + port, se.Message, null);
				}
				catch (Exception eeeee)
				{
					Console.WriteLine("e=" + eeeee);
					throw;
				}
			}

			var connected = await WaitForConnectAsync(cancellationToken).ConfigureAwait(false);
			if (!connected)
			{
				throw new InvalidOperationException(DisconnectReason ?? "Failed to connect to host.");
			}
		}

		/// <summary>
		/// Disconnect asynchronously.
		/// </summary>
		public Task DisconnectAsync(CancellationToken cancellationToken = default)
		{
			_ = StopKeepAliveAsync();
			return Task.Run(Disconnect, cancellationToken);
		}

		/// <summary>
		/// True-async version of WaitForConnect that polls connection flags without blocking a thread.
		/// </summary>
		public async Task<bool> WaitForConnectAsync(CancellationToken cancellationToken = default)
		{
			// Mimic WaitForConnect: loop until IsAnsi or Is3270 becomes true, otherwise fail if IsResolving false
			while (!IsAnsi && !Is3270)
			{
				cancellationToken.ThrowIfCancellationRequested();
				await Task.Delay(100, cancellationToken).ConfigureAwait(false);
				if (!IsResolving)
				{
					DisconnectReason = "Timeout waiting for connection";
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Send an output buffer asynchronously.
		/// </summary>
		public Task OutputAsync(NetBuffer buffer, CancellationToken cancellationToken = default)
		{
			if (buffer == null) throw new ArgumentNullException(nameof(buffer));
			return Task.Run(() => Output(buffer), cancellationToken);
		}

		public Task SendStringAsync(string s, CancellationToken cancellationToken = default)
		{
			return Task.Run(() => SendString(s), cancellationToken);
		}

		public Task SendCharAsync(char c, CancellationToken cancellationToken = default)
		{
			return Task.Run(() => SendChar(c), cancellationToken);
		}

		public Task SendByteAsync(byte b, CancellationToken cancellationToken = default)
		{
			return Task.Run(() => SendByte(b), cancellationToken);
		}

		public Task SendEraseAsync(CancellationToken cancellationToken = default)
		{
			return Task.Run(SendErase, cancellationToken);
		}

		public Task SendKillAsync(CancellationToken cancellationToken = default)
		{
			return Task.Run(SendKill, cancellationToken);
		}

		public Task SendWEraseAsync(CancellationToken cancellationToken = default)
		{
			return Task.Run(SendWErase, cancellationToken);
		}
	}
}
