#region License
/* 
 *
 * Open3270 - A C# implementation of the TN3270/TN3270E protocol
 *
 * Copyright (c) 2004-2020 Michael Warriner
 * Modifications (c) as per Git change history
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software
 * and associated documentation files (the "Software"), to deal in the Software without restriction,
 * including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
 * subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or substantial 
 * portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT 
 * LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
 
#endregion
using System;
using System.Net;
using System.Net.Sockets;

namespace Open3270.Library
{
	internal delegate void OnConnectionDelegate(ClientSocket sock);
	internal delegate void OnConnectionDelegateRaw(Socket sock);


	/// <summary>
	/// Summary description for ServerSocket.
	/// </summary>
	internal partial class ServerSocket(ServerSocketType socketType)
	{
		public event OnConnectionDelegate OnConnect;
		public event OnConnectionDelegateRaw OnConnectRaw;
		private Socket _mSocket;
		private AsyncCallback _callbackProc ;

		public ServerSocket() : this(ServerSocketType.ClientServer)
		{
		}

		public void Close()
		{
			try
			{
				Console.WriteLine("ServerSocket.CLOSE");
				_mSocket.Close();
			}
			catch (Exception)
			{
				//NOOP
			}
			_mSocket = null;
		}
		public void Listen(int port)
		{
			//IPHostEntry lipa = Dns.Resolve("host.contoso.com");
			var lep = new IPEndPoint(IPAddress.Any, port);

			_mSocket				= new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			_mSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
			// Create New EndPoint
			// This is a non blocking IO
			_mSocket.Blocking		= false ;	

			_mSocket.Bind(lep);
			//
			_mSocket.Listen(1000);
			//
			// Assign Callback function to read from Asyncronous Socket
			_callbackProc	= ConnectCallback;
			//
			_mSocket.BeginAccept(_callbackProc, null);
		}
		private void ConnectCallback( IAsyncResult ar )
		{
			try
			{
				Socket newSocket;
				try
				{
					newSocket = _mSocket.EndAccept(ar);
				}
				catch (ObjectDisposedException)
				{
					
					//Console.WriteLine("Server socket error - ConnectCallback failed "+ee.Message);
					_mSocket = null;
					return;
				}

				try
				{
					Audit.WriteLine("Connection received - call OnConnect");
					//
					OnConnectRaw?.Invoke(newSocket);
					//
					if (OnConnect != null)
					{
						var socket = new ClientSocket(newSocket)
						{
							FxSocketType = socketType
						};
						OnConnect(socket);
					}

					// restart accept
					_mSocket.BeginAccept(_callbackProc, null);
				}
				catch (ObjectDisposedException)
				{
					newSocket.Close();
					newSocket.Dispose();
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception occured in AcceptCallback\n"+e);
					newSocket.Close();
					newSocket.Dispose();
				}
			}
			finally
			{
				// wait for the next incoming connection
				_mSocket?.BeginAccept(_callbackProc, null);
			}
		}
	}
}
