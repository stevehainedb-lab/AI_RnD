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

namespace Open3270.TN3270;

/// <summary>
///     Summary description for LogParser.
/// </summary>
public class Tn3270HostParser : IAudit
{
	private readonly Telnet _telnet;

	/// <summary>
	/// </summary>
	public Tn3270HostParser()
	{
		var config = new ConnectionConfig
		{
			HostName = "DUMMY_PARSER"
		};
		var api = new Tn3270Api();

		_telnet = new Telnet(api, this, config);
		_telnet.Trace.optionTraceAnsi = true;
		_telnet.Trace.optionTraceDS = true;
		_telnet.Trace.optionTraceDSN = true;
		_telnet.Trace.optionTraceEvent = true;
		_telnet.Trace.optionTraceNetworkData = true;
		_telnet.telnetDataEventOccurred += telnet_telnetDataEvent;

		_telnet.Connect(null, null, 0);
	}

	public ConnectionConfig Config => _telnet.Config;

	public string Status
	{
		get
		{
			var text = "";
			text += "kybdinhibit = " + _telnet.Keyboard.keyboardLock;
			return text;
		}
	}

	/// <summary>
	///     Parse a byte of host data
	/// </summary>
	/// <param name="ch"></param>
	public void Parse(byte ch)
	{
		if (!_telnet.ParseByte(ch))
			Console.WriteLine("Disconnect should occur next");
	}

	private void telnet_telnetDataEvent(object parentData, TNEvent eventType, string text)
	{
		Console.WriteLine("EVENT " + eventType + " " + text);
	}
	
	public void Write(string text)
	{
		WriteLine(text);
	}

	public void WriteLine(string text)
	{
		Console.Write(text);
	}

}
