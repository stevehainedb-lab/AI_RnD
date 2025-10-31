using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Open3270.Library
{
	/// <summary>
	/// Async extensions for ServerSocket. Mirrors existing Begin/End pattern with modern async APIs.
	/// </summary>
	internal partial class ServerSocket
	{
		/// <summary>
		/// Asynchronously begin listening on a TCP port and start accepting connections.
		/// </summary>
		public async Task ListenAsync(int port, CancellationToken cancellationToken = default)
		{
			var lep = new IPEndPoint(IPAddress.Any, port);

			_mSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
			{
				Blocking = false
			};
			_mSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
			_mSocket.Bind(lep);
			_mSocket.Listen(1000);

			await StartAcceptLoopAsync(cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Starts an asynchronous accept loop using Socket.AcceptAsync.
		/// </summary>
		public async Task StartAcceptLoopAsync(CancellationToken cancellationToken = default)
		{
			// Loop until cancellation or socket disposed
			while (!cancellationToken.IsCancellationRequested)
			{
				Socket newSocket = null;
				try
				{
					var socket = _mSocket;
					if (socket == null)
					{
						return; // closed
					}
					newSocket = await socket.AcceptAsync(cancellationToken).ConfigureAwait(false);

					Audit.WriteLine("Connection received - call OnConnect (async)");
					OnConnectRaw?.Invoke(newSocket);

					if (OnConnect != null)
					{
						var client = new ClientSocket(newSocket)
						{
							FxSocketType = socketType
						};
						OnConnect(client);
					}
				}
				catch (OperationCanceledException)
				{
					newSocket?.Dispose();
					break;
				}
				catch (ObjectDisposedException)
				{
					newSocket?.Dispose();
					break; // listener closed
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception occurred in StartAcceptLoopAsync: " + e);
					newSocket?.Dispose();
				}
			}
		}
	}
}
