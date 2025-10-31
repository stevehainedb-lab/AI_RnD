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
using System.Collections.Generic;
using System.Text;

namespace Open3270.TN3270;

public partial class Tn3270Api : IDisposable
{
	#region Private Methods

	private void tn_CursorLocationChanged(object sender, EventArgs e)
	{
		OnCursorLocationChanged(e);
	}

	#endregion

	#region Events and Delegates

	public event RunScriptDelegate RunScriptRequested;
	public event OnDisconnectDelegate Disconnected;
	public event EventHandler CursorLocationChanged;

	#endregion Events


	#region Fields

	private Telnet _tn;

	private bool _debug;
	private bool _isDisposed;

	private string _sourceIp = string.Empty;

	#endregion Fields


	#region Properties

	/// <summary>
	///     Gets or sets whether or not we are using SSL.
	/// </summary>
	public bool UseSsl { get; set; }

	/// <summary>
	///     Returns whether or not the session is connected.
	/// </summary>
	public bool IsConnected
	{
		get
		{
			if (_tn != null && _tn.IsSocketConnected)
				return true;
			return false;
		}
	}

	/// <summary>
	///     Sets the value of debug.
	/// </summary>
	public bool Debug
	{
		set => _debug = value;
	}

	/// <summary>
	///     Returns the state of the keyboard.
	/// </summary>
	public int KeyboardLock => _tn.Keyboard.keyboardLock;

	/// <summary>
	///     Returns the cursor's current X position.
	/// </summary>
	public int CursorX
	{
		get
		{
			lock (_tn)
			{
				return _tn.Controller.CursorX;
			}
		}
	}

	/// <summary>
	///     Returns the cursor's current Y positon.
	/// </summary>
	public int CursorY
	{
		get
		{
			lock (_tn)
			{
				return _tn.Controller.CursorY;
			}
		}
	}

	/// <summary>
	///     Returns the text of the last exception thrown.
	/// </summary>
	public string LastException => _tn.Events.GetErrorAsText();

	internal Tn3270Api()
	{
		_tn = null;
	}

	internal string DisconnectReason
	{
		get
		{
			if (_tn != null) return _tn.DisconnectReason;
			return null;
		}
	}

	internal bool ShowParseError
	{
		set
		{
			if (_tn != null) _tn.ShowParseError = value;
		}
	}

	#endregion Properties


	#region Ctors, dtors, and clean-up

	~Tn3270Api()
	{
		Dispose(false);
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_isDisposed)
		{
			_isDisposed = true;
			if (disposing)
			{
				Disconnect();
				Disconnected = null;
				RunScriptRequested = null;
				if (_tn != null)
				{
					_tn.telnetDataEventOccurred -= tn_DataEventReceived;
					_tn.Dispose();
				}
			}
		}
	}

	#endregion Ctors, dtors, and clean-up


	#region Public Methods

	/// <summary>
	///     Connects to host using a local IP
	///     If a source IP is given then use it for the local IP
	/// </summary>
	/// <param name="audit">IAudit interface to post debug/tracing to</param>
	/// <param name="localIp">ip to use for local end point</param>
	/// <param name="host">host ip/name</param>
	/// <param name="port">port to use</param>
	/// <param name="config">configuration parameters</param>
	/// <returns></returns>
	public bool Connect(IAudit audit, string localIp, string host, int port, ConnectionConfig config)
	{
		_sourceIp = localIp;
		return Connect(audit, host, port, string.Empty, config);
	}

	/// <summary>
	///     Connects a Telnet object to the host using the parameters provided
	/// </summary>
	/// <param name="audit">IAudit interface to post debug/tracing to</param>
	/// <param name="host">host ip/name</param>
	/// <param name="port">port to use</param>
	/// <param name="lu">lu to use or empty string for host negotiated</param>
	/// <param name="config">configuration parameters</param>
	/// <returns></returns>
	public bool Connect(IAudit audit, string host, int port, string lu, ConnectionConfig config)
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

		if (lu == null || lu.Length == 0)
		{
			_tn.Lus = null;
		}
		else
		{
			_tn.Lus = new List<string>();
			_tn.Lus.Add(lu);
		}

		if (!string.IsNullOrEmpty(_sourceIp))
			_tn.Connect(this, host, port, _sourceIp);
		else
			_tn.Connect(this, host, port);

		if (!_tn.WaitForConnect())
		{
			_tn.Disconnect();
			var text = _tn.DisconnectReason;
			_tn = null;
			throw new TnHostException("connect to " + host + " on port " + port + " failed", text, null);
		}

		if (config.KeepAlivePeriod is { TotalSeconds: > 0 })
		{
			_tn.StartKeepAlive(config.KeepAlivePeriod.Value);
		}

		_tn.Trace.WriteLine("--connected");

		return true;
	}

	/// <summary>
	///     Disconnects the connected telnet object from the host
	/// </summary>
	public void Disconnect()
	{
		if (_tn != null)
		{
			_tn.Disconnect();
			_tn.CursorLocationChanged -= tn_CursorLocationChanged;
			_tn = null;
		}
	}

	/// <summary>
	///     Waits for the connection to complete.
	/// </summary>
	/// <param name="timeout"></param>
	/// <returns></returns>
	public bool WaitForConnect(int timeout)
	{
		var success = _tn.WaitFor(SmsState.ConnectWait, timeout);
		if (success)
			if (!_tn.IsConnected)
				success = false;

		return success;
	}

	/// <summary>
	///     Gets the entire contents of the screen.
	/// </summary>
	/// <param name="crlf"></param>
	/// <returns></returns>
	public string GetAllStringData(bool crlf = false)
	{
		lock (_tn)
		{
			var builder = new StringBuilder();
			var index = 0;
			string temp;
			while ((temp = _tn.Action.GetStringData(index)) != null)
			{
				builder.Append(temp);
				if (crlf) builder.Append("\n");
				index++;
			}

			return builder.ToString();
		}
	}

	/// <summary>
	///     Sends an operator key to the mainframe.
	/// </summary>
	/// <param name="op"></param>
	/// <returns></returns>
	public bool SendKeyOp(KeyboardOp op)
	{
		bool success;
		lock (_tn)
		{
			// These can go to a locked screen		
			if (op == KeyboardOp.Reset)
			{
				success = true;
			}
			else
			{
				if ((_tn.Keyboard.keyboardLock & KeyboardConstants.OiaMinus) != 0 ||
				    _tn.Keyboard.keyboardLock != 0)
					success = false;
				else
					// These need unlocked screen
					switch (op)
					{
						case KeyboardOp.AID:
						{
							var field = typeof(AID).GetField(op.ToString());
							var v = (byte)field?.GetValue(null)!;
							_tn.Keyboard.HandleAttentionIdentifierKey(v);
							success = true;
							break;
						}
						case KeyboardOp.Home:
						{
							if (_tn.IsAnsi)
							{
								Console.WriteLine("IN_ANSI Home key not supported");
								//ansi_send_home();
								return false;
							}

							if (!_tn.Controller.Formatted)
							{
								_tn.Controller.SetCursorAddress(0);
								return true;
							}

							_tn.Controller.SetCursorAddress(_tn.Controller.GetNextUnprotectedField(_tn.Controller.RowCount * _tn.Controller.ColumnCount - 1));
							success = true;
							break;
						}
						case KeyboardOp.ATTN:
						{
							_tn.Interrupt();
							success = true;
							break;
						}
						default:
						{
							throw new ApplicationException("Sorry, key '" + op + "'not known");
						}
					}
			}
		}

		return success;
	}

	/// <summary>
	///     Gets the text at the specified cursor position.
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="length"></param>
	/// <returns></returns>
	public string GetText(int x, int y, int length)
	{
		MoveCursor(CursorOp.Exact, x, y);
		lock (_tn)
		{
			_tn.Controller.MoveCursor(CursorOp.Exact, x, y);
			return _tn.Action.GetStringData(length);
		}
	}

	/// <summary>
	///     Sets the text to the specified value at the specified position.
	/// </summary>
	/// <param name="text"></param>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="paste"></param>
	public void SetText(string text, int x, int y, bool paste = true)
	{
		MoveCursor(CursorOp.Exact, x, y);
		lock (_tn)
		{
			SetText(text, paste);
		}
	}

	/// <summary>
	///     Sets the text value at the current cursor position.
	/// </summary>
	/// <param name="text"></param>
	/// <param name="paste"></param>
	/// <returns></returns>
	public bool SetText(string text, bool paste = true)
	{
		lock (_tn)
		{
			var success = true;
			int i;
			if (text != null)
				for (i = 0; i < text.Length; i++)
				{
					success = _tn.Keyboard.HandleOrdinaryCharacter(text[i], false, paste);
					if (!success) break;
				}

			return success;
		}
	}

	/// <summary>
	///     Gets the field attributes of the field at the specified coordinates.
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <returns></returns>
	public FieldAttributes GetFieldAttribute(int x, int y)
	{
		byte b;
		lock (_tn)
		{
			b = (byte)_tn.Controller.GetFieldAttribute(_tn.Controller.CursorAddress);
		}

		var fa = new FieldAttributes();
		fa.IsHigh = FieldAttribute.IsHigh(b);
		fa.IsIntense = FieldAttribute.IsIntense(b);
		fa.IsModified = FieldAttribute.IsModified(b);
		fa.IsNormal = FieldAttribute.IsNormal(b);
		fa.IsNumeric = FieldAttribute.IsNumeric(b);
		fa.IsProtected = FieldAttribute.IsProtected(b);
		fa.IsSelectable = FieldAttribute.IsSelectable(b);
		fa.IsSkip = FieldAttribute.IsSkip(b);
		fa.IsZero = FieldAttribute.IsZero(b);
		return fa;
	}

	/// <summary>
	///     Moves the cursor to the specified position.
	/// </summary>
	/// <param name="op"></param>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <returns></returns>
	public bool MoveCursor(CursorOp op, int x, int y)
	{
		lock (_tn)
		{
			return _tn.Controller.MoveCursor(op, x, y);
		}
	}

	/// <summary>
	///     Returns the text of the last erFror thrown.
	/// </summary>
	/// <returns></returns>
	public bool ExecuteAction(bool submit, string name, params object[] args)
	{
		lock (_tn)
		{
			return _tn.Action.Execute(submit, name, args);
		}
	}

	public bool KeyboardCommandCausesSubmit(string name)
	{
		return _tn.Action.KeyboardCommandCausesSubmit(name);
	}

	public bool Wait(int timeout)
	{
		return _tn.WaitFor(SmsState.KBWait, timeout);
	}

	public void RunScript(string where)
	{
		RunScriptRequested?.Invoke(where);
	}

	#endregion Public Methods


	#region Depricated Methods

	[Obsolete(
		"This method has been deprecated.  Please use SendKeyOp(KeyboardOp op) instead. This method is only included for backwards compatibiity and might not exist in future releases.")]
	public bool SendKeyOp(KeyboardOp op, string key)
	{
		return SendKeyOp(op);
	}

	[Obsolete("This method has been deprecated.  Please use SetText instead. This method is only included for backwards compatibiity and might not exist in future releases.")]
	public bool SendText(string text, bool paste)
	{
		return SetText(text, paste);
	}

	[Obsolete("This method has been deprecated.  Please use GetText instead. This method is only included for backwards compatibiity and might not exist in future releases.")]
	public string GetStringData(int index)
	{
		lock (_tn)
		{
			return _tn.Action.GetStringData(index);
		}
	}

	[Obsolete(
		"This method has been deprecated.  Please use LastException instead. This method is only included for backwards compatibiity and might not exist in future releases.")]
	public string GetLastError()
	{
		return LastException;
	}

	#endregion


	#region Eventhandlers and such

	private void tn_DataEventReceived(object parentData, TNEvent eventType, string text)
	{
		//Console.WriteLine("event = "+eventType+" text='"+text+"'");
		if (eventType == TNEvent.Disconnect) Disconnected?.Invoke(null, "Client disconnected session");
		if (eventType == TNEvent.DisconnectUnexpected) Disconnected?.Invoke(null, "Host disconnected session");
	}


	protected virtual void OnCursorLocationChanged(EventArgs args)
	{
		CursorLocationChanged?.Invoke(this, args);
	}

	#endregion Eventhandlers and such
}
