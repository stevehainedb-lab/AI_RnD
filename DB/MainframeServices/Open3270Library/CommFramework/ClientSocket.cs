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
	internal delegate void ClientSocketNotify(string eventName, Message message);
	internal delegate void ClientDataNotify(byte[] data, int Length);
	
	
		/// <summary>
		/// Summary description for ClientSocket.
		/// </summary>
		internal partial class ClientSocket
	{
		private ServerSocketType _mSocketType = ServerSocketType.ClientServer;
		private enum State
		{
			Waiting,
			ReadingHeader,
			ReadingBuffer
		}
		private IPEndPoint _iep ;
		private AsyncCallback _callbackProc ;
		private Socket _mSocket ;
		private readonly Byte[] _mByBuff = new Byte[32767];

		// Code for message builder
		private byte[] _currentBuffer;
		private int _currentBufferIndex;
		private MessageHeader _currentMessageHeader;
		private State _mState;

		public event ClientSocketNotify OnNotify;
		public event ClientDataNotify OnData;

		private void Info()
		{
			Audit.WriteLine("CLRVersion = "+Environment.Version);
			Audit.WriteLine("UserName   = "+Environment.UserName);
			Audit.WriteLine("Assembly version = "+typeof(ClientSocket).Assembly.FullName);
		}
		public ClientSocket()
		{
			Audit.WriteLine("Client socket created.");
			Info();
		}
		public ClientSocket(Socket sock)
		{
			_mSocket = sock;
			Info();
		}
		public ServerSocketType FxSocketType
		{
			get => _mSocketType;
			set => _mSocketType = value;
		}
		public void Start()
		{
			Audit.WriteLine("sock.start");
			if (_mSocket.Connected)
			{
				_mSocket.Blocking = false;
				_mState = State.Waiting;
				AsyncCallback receiveData = OnReceivedData;
				_mSocket.BeginReceive(_mByBuff, 0, _mByBuff.Length, SocketFlags.None, receiveData , _mSocket );
				Audit.WriteLine("called begin receive");
			}
			else
			{
				Audit.WriteLine("bugbug-- not connected");
				throw new ApplicationException("Socket passed to us, but not connected to anything");
			}
		}
		public void Connect(string address, int port)
		{
			Audit.WriteLine("Connect "+address+" -- "+port);
			Disconnect();
			_mState = State.Waiting;

			//
			// Actually connect
			//
			// count .s, numeric and 3 .s =ipaddress
			var ipaddress=false;
			var text = false;
			var count =0 ;
			int i;
			for (i=0; i<address.Length; i++)
			{
				if (address[i]=='.')
					count++;
				else
				{
					if (address[i]<'0' || address[i]>'9')
						text = true;
				}
			}
			if (count==3 && text==false)
				ipaddress = true;

			if (!ipaddress)
			{
				Audit.WriteLine("Dns.Resolve " + address);
				var ipHost = Dns.GetHostEntry(address);
				var ipAddress = Array.Find(ipHost.AddressList, a => a.AddressFamily == AddressFamily.InterNetwork);
				if (ipAddress == null)
				{
					throw new ApplicationException("Unable to resolve an IPv4 address for host '" + address + "'");
				}
				_iep                = new IPEndPoint(ipAddress, port);  
			}
			else
			{
				Audit.WriteLine("Use address "+address+" as ip");
				_iep				= new IPEndPoint(IPAddress.Parse(address),port);  
			}

		
			try
			{
				// Create New Socket 
				_mSocket				= new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				_mSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
				// Create New EndPoint
				// This is a non blocking IO
				_mSocket.Blocking		= false ;	
				// set some random options
				//
				//mSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, 1);
			
				//
				// Assign Callback function to read from Asyncronous Socket
				_callbackProc	= ConnectCallback;
				// Begin Asyncronous Connection
				_mSocket.BeginConnect(_iep , _callbackProc, _mSocket ) ;				
		
			}
			catch(Exception eeeee )
			{
				Audit.WriteLine("e="+eeeee);
				throw;
				//				st_changed(STCALLBACK.ST_CONNECT, false);
			}
		}
		public void Disconnect()
		{
			if (_mSocket != null)
			{
				var mTemp = _mSocket;
				_mSocket = null;
				Audit.WriteLine("start close");
				
			
				try
				{
					mTemp.Blocking = true;

					if (mTemp.Connected)
						mTemp.Close();
				}
				catch (Exception)
				{
					//noop
				}
				Audit.WriteLine("stop close - all async handlers should be disconnected by now");
				OnNotify?.Invoke("Disconnect", null);
			}
			_mSocket = null;
		}
		public bool IsConnected
		{
			get 
			{ 
				if (_mSocket==null)
					return false;
				if (!_mSocket.Connected)
					return false;
				return true;
			}
		}
		private void ConnectCallback( IAsyncResult ar )
		{
			try
			{
				Audit.WriteLine("connect async notifier");
				// Get The connection socket from the callback
				var sock1 = (Socket)ar.AsyncState;
				if ( sock1 is { Connected: true } ) 
				{	
					// notify parent here
					OnNotify?.Invoke("Connect", null);
					//
					// Define a new Callback to read the data 
					AsyncCallback receiveData = OnReceivedData;
					// Begin reading data asyncronously
					sock1.BeginReceive( _mByBuff, 0, _mByBuff.Length, SocketFlags.None, receiveData , sock1 );
					Audit.WriteLine("setup data receiver");
				}
				else
				{
					// notify parent - connect failed
					OnNotify?.Invoke("ConnectFailed", null);
					Audit.WriteLine("Connect failed");
				}
			}
			catch( Exception ex )
			{
				Audit.WriteLine("Setup Receive callback failed "+ex);
				throw;
			}
		}
		
		private void OnReceivedData( IAsyncResult ar )
		{
			//Audit.WriteLine("OnReceivedData");
			// Get The connection socket from the callback
			var sock = (Socket)ar.AsyncState;
			// is socket closing
			if (sock is not { Connected: true })
				return; 
			// Get The data , if any
			int nBytesRec;
			try
			{
				nBytesRec = sock.EndReceive( ar );	
			}
			catch (ObjectDisposedException)
			{
				Console.WriteLine("Client socket OnReceived data received socket disconnect (object disposed)");
				OnData?.Invoke(null,0); // notify close
				return;
			}
			catch (SocketException se)
			{
				Console.WriteLine("Client socket OnReceived data received socket disconnect ("+se.Message+")");
				OnData?.Invoke(null,0); // notify close
				return;
			}
			//Audit.WriteLine("OnReceivedData bytes="+nBytesRec);
			if (OnData != null && nBytesRec > 0 )
			{
				OnData(_mByBuff, nBytesRec);
			}
			if( nBytesRec > 0 )
			{

				// process it if we're a client server socket
				if (_mSocketType==ServerSocketType.ClientServer)
				{

					for (var i=0; i<nBytesRec; i++)
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
									Audit.WriteLine("RCLRVersion = "+Environment.Version);
									Audit.WriteLine("Writeline "+dump);
									try
									{
								
										var msg = Message.CreateFromByteArray(_currentBuffer);
										if (msg != null && OnNotify != null)
											OnNotify("Data", msg);
									}
									catch (Exception e)
									{
										Audit.WriteLine("Exception handling message "+e);
									}
									_mState = State.Waiting;
								}
								break;

							default:
								throw new ApplicationException("sorry, state '"+_mState+"' not known");
						}
					}
				}
				// 
				//
				try
				{
					// Define a new Callback to read the data 
					AsyncCallback receiveData = OnReceivedData;
					// Begin reading data asyncronously
					_mSocket.BeginReceive( _mByBuff, 0, _mByBuff.Length, SocketFlags.None, receiveData , _mSocket );
					//
				}
				catch (Exception e)
				{
					// assume socket was disconnected somewhere else if an exception occurs here
					Audit.WriteLine( "Socket BeginReceived failed with error "+e.Message);
					Disconnect();
				}
			}
			else
			{
				// If no data was received then the connection is probably dead
				Audit.WriteLine( "Socket was disconnected disconnected");//+ sock.RemoteEndPoint );
				Disconnect();
			}
		}
		public void Send(Message msg)
		{
			if (_mSocket.Connected==false)
				throw new ApplicationException("Sorry, socket is not connected");
			//
			msg.Send(_mSocket);
		}
		public void Send(byte[] data)
		{
			if (_mSocket.Connected==false)
				throw new ApplicationException("Sorry, socket is not connected");
			//
			_mSocket.Send(data);
		}
		public void Send(byte[] data, int length)
		{
			if (_mSocket.Connected==false)
				throw new ApplicationException("Sorry, socket is not connected");
			//
			_mSocket.Send(data, 0, length, SocketFlags.None);
		}

	}
}
