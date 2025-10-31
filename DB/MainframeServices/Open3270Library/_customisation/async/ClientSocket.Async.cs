using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Open3270.Library
{
	internal partial class ClientSocket
	{
		// Async counterparts to allow modern async flows without changing existing behavior
		public async Task ConnectAsync(string address, int port, CancellationToken cancellationToken = default)
		{
			Audit.WriteLine("ConnectAsync " + address + " -- " + port);
			Disconnect();
			_mState = State.Waiting;

			bool isIp = IPAddress.TryParse(address, out var ip);
			if (!isIp)
			{
				Audit.WriteLine("Dns.GetHostEntryAsync " + address);
				var ipHost = await Dns.GetHostEntryAsync(address, cancellationToken).ConfigureAwait(false);
				ip = Array.Find(ipHost.AddressList, a => a.AddressFamily == AddressFamily.InterNetwork);
				if (ip == null)
				{
					throw new ApplicationException("Unable to resolve an IPv4 address for host '" + address + "'");
				}
			}
			_iep = new IPEndPoint(ip, port);

			_mSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
			{
				Blocking = false
			};
			_mSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

			try
			{
				await _mSocket.ConnectAsync(_iep, cancellationToken).ConfigureAwait(false);
				if (_mSocket.Connected)
				{
					OnNotify?.Invoke("Connect", null);
				}
				else
				{
					OnNotify?.Invoke("ConnectFailed", null);
				}
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception e)
			{
				Audit.WriteLine("Async connect failed: " + e);
				OnNotify?.Invoke("ConnectFailed", null);
				throw;
			}
		}

		public async Task StartReceiveLoopAsync(CancellationToken cancellationToken = default)
		{
			Audit.WriteLine("sock.start (async)");
			if (_mSocket == null || !_mSocket.Connected)
			{
				Audit.WriteLine("bugbug-- not connected");
				throw new ApplicationException("Socket passed to us, but not connected to anything");
			}
			_mSocket.Blocking = false;
			_mState = State.Waiting;

			while (_mSocket != null && _mSocket.Connected && !cancellationToken.IsCancellationRequested)
			{
				int nBytesRec;
				try
				{
					nBytesRec = await _mSocket.ReceiveAsync(_mByBuff, SocketFlags.None, cancellationToken).ConfigureAwait(false);
				}
				catch (OperationCanceledException)
				{
					break;
				}
				catch (ObjectDisposedException)
				{
					Console.WriteLine("Client socket async receive: socket disposed");
					OnData?.Invoke(null, 0);
					break;
				}
				catch (SocketException se)
				{
					Console.WriteLine("Client socket async receive disconnect (" + se.Message + ")");
					OnData?.Invoke(null, 0);
					break;
				}

				if (OnData != null && nBytesRec > 0)
				{
					OnData(_mByBuff, nBytesRec);
				}

				if (nBytesRec > 0)
				{
					if (_mSocketType == ServerSocketType.ClientServer)
					{
						for (var i = 0; i < nBytesRec; i++)
						{
							switch (_mState)
							{
								case State.Waiting:
									_currentBuffer = new byte[MessageHeader.MessageHeaderSize];
									_currentBufferIndex = 0;
									_currentBuffer[_currentBufferIndex++] = _mByBuff[i];
									_mState = State.ReadingHeader;
									break;
								case State.ReadingHeader:
									_currentBuffer[_currentBufferIndex++] = _mByBuff[i];
									if (_currentBufferIndex >= MessageHeader.MessageHeaderSize)
									{
										_currentMessageHeader = new MessageHeader(_currentBuffer);
										_currentBuffer = new byte[_currentMessageHeader.UMessageSize];
										_currentBufferIndex = 0;
										_mState = State.ReadingBuffer;
									}
									break;
								case State.ReadingBuffer:
									_currentBuffer[_currentBufferIndex++] = _mByBuff[i];
									if (_currentBufferIndex >= _currentMessageHeader.UMessageSize)
									{
										var dump = System.Text.Encoding.ASCII.GetString(_currentBuffer);
										Audit.WriteLine("RCLRVersion = " + Environment.Version);
										Audit.WriteLine("Writeline " + dump);
										try
										{
var msg = await Message.CreateFromByteArrayAsync(_currentBuffer, cancellationToken).ConfigureAwait(false);
											if (msg != null && OnNotify != null)
												OnNotify("Data", msg);
										}
										catch (Exception e)
										{
											Audit.WriteLine("Exception handling message " + e);
										}
										_mState = State.Waiting;
									}
									break;
								default:
									throw new ApplicationException("sorry, state '" + _mState + "' not known");
							}
						}
					}
				}
				else
				{
					Audit.WriteLine("Socket was disconnected (async)");
					Disconnect();
					break;
				}
			}
		}

		public async Task SendAsync(Message msg, CancellationToken cancellationToken = default)
		{
			if (_mSocket == null || !_mSocket.Connected)
				throw new ApplicationException("Sorry, socket is not connected");

			// Properly chain to Message.SendAsync to reuse framing and serializer logic.
			await msg.SendAsync(_mSocket, cancellationToken).ConfigureAwait(false);
		}

		public async Task SendAsync(byte[] data, CancellationToken cancellationToken = default)
		{
			if (_mSocket == null || !_mSocket.Connected)
				throw new ApplicationException("Sorry, socket is not connected");
			await _mSocket.SendAsync(data, SocketFlags.None, cancellationToken).ConfigureAwait(false);
		}

		public async Task SendAsync(byte[] data, int length, CancellationToken cancellationToken = default)
		{
			if (_mSocket == null || !_mSocket.Connected)
				throw new ApplicationException("Sorry, socket is not connected");
			await _mSocket.SendAsync(new ReadOnlyMemory<byte>(data, 0, length), SocketFlags.None, cancellationToken).ConfigureAwait(false);
		}
	}
}

																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																							







																																																																																																																																																																																																																																																	





																																																																																																																																																																																																																																	


																																																																																																																																																																																																																																																																																																																																																				
