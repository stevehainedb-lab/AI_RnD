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
using System.Collections;
using System.Net.Sockets;
using Open3270.Library;

namespace Open3270.TN3270Server;

/// <summary>
///     Summary description for TN3270ServerEmulationBase.
/// </summary>
public abstract class Tn3270ServerEmulationBase
{
	private readonly Queue _mData = new();
	private readonly MySemaphore _mDataSemaphore = new(0, 9999);

	private ClientSocket _mSocket;

	//

	public bool Tn3270E { get; set; }

	public void Init(Socket sock)
	{
		_mSocket = new ClientSocket(sock);
		_mSocket.FxSocketType = ServerSocketType.RAW;
		_mSocket.OnData += cs_OnData;
		_mSocket.OnNotify += mSocket_OnNotify;
		_mSocket.Start();
	}

	public virtual void Run()
	{
	}

	public abstract Tn3270ServerEmulationBase CreateInstance(Socket sock);

	private void cs_OnData(byte[] data, int length)
	{
		if (data == null)
		{
			Console.WriteLine("cs_OnData received disconnect, close down this instance");
			Disconnect();
		}
		else
		{
			//Console.WriteLine("\n\n--on data");
			AddData(data, length);
		}
	}

	public void Send(string dataStream)
	{
		//Console.WriteLine("send "+dataStream);
		var bytedata = ByteFromData(dataStream);
		_mSocket.Send(bytedata);
	}

	public void Send(TnServerScreen s)
	{
		//Console.WriteLine("send screen");
		var data = s.AsTn3270Buffer(true, true, Tn3270E);
		//Console.WriteLine("data.length="+data.Length);
		_mSocket.Send(data);
	}

	private byte[] ByteFromData(string text)
	{
		var data = text.Split(new[] { ' ' });
		var bytedata = new byte[data.Length];
		for (var i = 0; i < data.Length; i++) bytedata[i] = (byte)Convert.ToInt32(data[i], 16);
		return bytedata;
	}

	public string WaitForKey(TnServerScreen currentScreen)
	{
		//Console.WriteLine("--wait for key");
		byte[] data;
		bool screen;
		do
		{
			do
			{
				while (!_mDataSemaphore.Acquire(1000))
					if (_mSocket == null)
						throw new Tn3270ServerException("Connection dropped");
				data = (byte[])_mData.Dequeue();
			} while (data == null);

			if (data[0] == 255 && data[1] == 253)
			{
				// assume do/will string
				for (var i = 0; i < data.Length; i++)
					if (data[i] == 253)
					{
						data[i] = 251; // swap DO to WILL
						Console.WriteLine("DO " + data[i + 1]);
					}
					else if (data[i] == 251)
					{
						data[i] = 253; // swap WILL to DO
						Console.WriteLine("WILL " + data[i + 1]);
					}

				_mSocket.Send(data);
				screen = false;
			}
			else
			{
				screen = true;
			}
		} while (!screen);

		return currentScreen.HandleTn3270Data(data, data.Length);
	}

	public int Wait(params string[] dataStream)
	{
		//Console.WriteLine("--wait for "+dataStream);
		while (!_mDataSemaphore.Acquire(1000))
			if (_mSocket == null)
				throw new Tn3270ServerException("Connection dropped");
		var data = (byte[])_mData.Dequeue();

		if (data == null || data.Length == 0)
		{
			return 0;
		}
			
		for (var count = 0; count < dataStream.Length; count++)
		{
			var bytedata = ByteFromData(dataStream[count]);

			if (bytedata.Length == data.Length)
			{
				var ok = true;
				for (var i = 0; i < bytedata.Length; i++)
					if (bytedata[i] != data[i])
						ok = false;

				if (ok)
					return count;
			}
		}

		Console.WriteLine("\n\ndata match error");
		for (var count = 0; count < dataStream.Length; count++) Console.WriteLine("--expected " + dataStream);
		Console.Write("--received ");
		for (var i = 0; i < data.Length; i++) Console.Write("{0:x2} ", data[i]);
		Console.WriteLine();
		throw new Tn3270ServerException("Error reading incoming data stream. Expected data missing. Check console log for details");
	}

	public void AddData(byte[] data, int length)
	{
		var copy = new byte[length];
		for (var i = 0; i < length; i++) copy[i] = data[i];
		_mData.Enqueue(copy);
		_mDataSemaphore.Release();
	}

	public void Disconnect()
	{
		if (_mSocket != null)
		{
			_mSocket.Disconnect();
			_mSocket = null;
		}
	}

	private void mSocket_OnNotify(string eventName, Message message)
	{
		if (eventName == "Disconnect") Disconnect();
	}
}
