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
using System.Linq;
using System.Reflection;
using System.Threading;
using Open3270.Interfaces;
using Open3270.Library;
using Open3270.TN3270;

namespace Open3270;

/// <summary>
///     Summary description for TNEmulator.
/// </summary>
public partial class TnEmulator : IDisposable, ITnEmulator
{
	#region Private Variables and Objects

	private static bool _firstTime = true;

	private readonly MySemaphore _semaphore = new(0, 9999);
	
	private Tn3270Api _currentConnection;

	#endregion

	#region Event Handlers

	/// <summary>
	///     Event fired when the host disconnects. Note - this must be set before you connect to the host.
	/// </summary>
	public event OnDisconnectDelegate Disconnected;

	public event EventHandler CursorLocationChanged;
	private OnDisconnectDelegate _apiOnDisconnectDelegate;

	#endregion

	#region Constructors / Destructors
	
	~TnEmulator()
	{
		Dispose(false);
	}

	#endregion

	#region Properties

	/// <summary>
	///     Returns whether this session is connected to the mainframe.
	/// </summary>
	public bool IsConnected
	{
		get
		{
			if (_currentConnection == null)
				return false;
			return _currentConnection.IsConnected;
		}
	}

	/// <summary>
	///     Gets or sets the ojbect state.
	/// </summary>
	public object ObjectState { get; set; }

	/// <summary>
	///     Returns whether the disposed action has been performed on this object.
	/// </summary>
	public bool IsDisposed { get; private set; }

	/// <summary>
	///     Returns the reason why the session has been disconnected.
	/// </summary>
	public string DisconnectReason
	{
		get
		{
			lock (this)
			{
				if (_currentConnection != null)
					return _currentConnection.DisconnectReason;
			}

			return string.Empty;
		}
	}

	/// <summary>
	///     Returns zero if the keyboard is currently locked (inhibited)
	///     non-zero otherwise
	/// </summary>
	public int KeyboardLocked
	{
		get
		{
			if (_currentConnection == null) throw new TnHostException("TNEmulator is not connected", "There is no currently open TN3270 connection", null);
			return _currentConnection.KeyboardLock;
		}
	}

	/// <summary>
	///     Returns the zero based X coordinate of the cursor
	/// </summary>
	public int CursorX
	{
		get
		{
			if (_currentConnection == null) throw new TnHostException("TNEmulator is not connected", "There is no currently open TN3270 connection", null);
			return _currentConnection.CursorX;
		}
	}

	/// <summary>
	///     Returns the zero based Y coordinate of the cursor
	/// </summary>
	public int CursorY
	{
		get
		{
			if (_currentConnection == null) throw new TnHostException("TNEmulator is not connected", "There is no currently open TN3270 connection", null);
			return _currentConnection.CursorY;
		}
	}

	/// <summary>
	///     Returns the IP address of the mainframe.
	/// </summary>
	public string LocalIp { get; private set; } = string.Empty;

	/// <summary>
	///     Returns the internal configuration object for this connection
	/// </summary>
	public ConnectionConfig Config { get; init; } = new ConnectionConfig();

	/// <summary>
	///     Debug flag - setting this to true turns on much more debugging output on the
	///     Audit output
	/// </summary>
	public bool Debug { get; set; }

	/// <summary>
	///     Set this flag to true to enable SSL connections. False otherwise
	/// </summary>
	public bool UseSsl { get; set; }

	private IScreen _currentScreen; // don't access me directly, use helper
	/// <summary>
	///     Returns the current screen XML
	/// </summary>
	public IScreen CurrentScreen
	{
		get
		{
			if (_currentScreen == null)
			{
				if (Audit != null && Debug)
				{
					Audit.WriteLine("CurrentScreenXML reloading by calling GetScreenAsXML()");
					_currentScreen = GetScreenAsXml();
					_currentScreen.Dump(Audit);
				}
				else
				{
					//
					_currentScreen = GetScreenAsXml();
				}
			}

			//
			return _currentScreen;
		}
	}

	protected void DisposeOfCurrentScreenXml()
	{
		if (_currentScreen == null) return;
		var disposeXml = _currentScreen as IDisposable;
		disposeXml?.Dispose();
		_currentScreen = null;
	}
	
	bool ITnEmulator.IsConnected { get; set; }

	/// <summary>
	///     Auditing interface
	/// </summary>
	public IAudit Audit { get; set; }

	#endregion

	#region Public Methods

	/// <summary>
	///     Disposes of this emulator object.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	///     Sends the specified key stroke to the emulator.
	/// </summary>
	/// <param name="waitForScreenToUpdate"></param>
	/// <param name="key"></param>
	/// <param name="timeoutMs"></param>
	/// <returns></returns>
	public bool SendKey(bool waitForScreenToUpdate, TnKey key, int timeoutMs)
	{
		string command;

		//This is only used as a parameter for other methods when we're using function keys.
		//e.g. F1 yields a command of "PF" and a functionInteger of 1.
		var functionInteger = -1;


		if (Audit != null && Debug) Audit.WriteLine("\nSendKeyFromText(" + waitForScreenToUpdate + ", \"" + key + "\", " + timeoutMs + ")");

		if (_currentConnection == null) throw new TnHostException("TNEmulator is not connected", "There is no currently open TN3270 connection", null);


		//Get the command name and accompanying int parameter, if applicable
		if (Constants.FunctionKeys.Contains(key))
		{
			command = "PF";
			functionInteger = Constants.FunctionKeyIntLut[key];
		}
		else if (Constants.AKeys.Contains(key))
		{
			command = "PA";
			functionInteger = Constants.FunctionKeyIntLut[key];
		}
		else
		{
			command = key.ToString();
		}

		//Should this action be followed by a submit?
		var triggerSubmit = Config.SubmitAllKeyboardCommands || _currentConnection.KeyboardCommandCausesSubmit(command);

		if (triggerSubmit)
			lock (this)
			{
				DisposeOfCurrentScreenXml();

				if (Audit != null && Debug) Audit.WriteLine("mre.Reset. Count was " + _semaphore.Count);

				// Clear to initial count (0)
				_semaphore.Reset();
			}

		var success = _currentConnection.ExecuteAction(triggerSubmit, command, functionInteger);


		if (Audit != null && Debug) Audit.WriteLine("\nSendKeyFromText - submit = " + triggerSubmit + " ok=" + success);

		if (triggerSubmit && success)
			// Wait for a valid screen to appear
			if (waitForScreenToUpdate)
				success = Refresh(true, timeoutMs);

		return success;
	}

	/// <summary>
	///     Waits until the keyboard state becomes unlocked.
	/// </summary>
	/// <param name="timeoutms"></param>
	public void WaitTillKeyboardUnlocked(int timeoutms)
	{
		var dttm = DateTime.Now.AddMilliseconds(timeoutms);

		while (KeyboardLocked != 0 && DateTime.Now < dttm) Thread.Sleep(10); // Wait 1/100th of a second
	}

	/// <summary>
	///     Refresh the current screen.  If timeout > 0, it will wait for
	///     this number of milliseconds.
	///     If waitForValidScreen is true, it will wait for a valid screen, otherwise it
	///     will return immediately that any screen data is visible
	/// </summary>
	/// <param name="waitForValidScreen"></param>
	/// <param name="timeoutMs">The time to wait in ms</param>
	/// <returns></returns>
	public bool Refresh(bool waitForValidScreen, int timeoutMs)
	{
		var start = DateTime.Now.Ticks / (10 * 1000);
		var end = start + timeoutMs;

		if (_currentConnection == null) throw new TnHostException("TNEmulator is not connected", "There is no currently open TN3270 connection", null);

		if (Audit != null && Debug) Audit.WriteLine("Refresh::Refresh(" + waitForValidScreen + ", " + timeoutMs + "). FastScreenMode=" + Config.FastScreenMode);

		do
		{
			if (waitForValidScreen)
			{
				int timeout;
				do
				{
					timeout = (int)(end - DateTime.Now.Ticks / 10000);
					if (timeout > 0)
					{
						if (Audit != null && Debug) Audit.WriteLine("Refresh::Acquire(" + timeout + " milliseconds). unsafe Count is currently " + _semaphore.Count);

						var run = _semaphore.Acquire(Math.Min(timeout, 1000));

						if (!IsConnected) throw new TnHostException("The TN3270 connection was lost", _currentConnection.DisconnectReason, null);

						if (run)
						{
							if (Audit != null && Debug) Audit.WriteLine("Refresh::return true at line 279");
							return true;
						}
					}
				} while (timeout > 0);

				if (Audit != null && Debug) Audit.WriteLine("Refresh::Timeout or acquire failed. run=false timeout=" + timeout);
			}

			if (Config.FastScreenMode || KeyboardLocked == 0)
			{
				// Store screen in screen database and identify it
				DisposeOfCurrentScreenXml();
				if (Audit != null && Debug) Audit.WriteLine("Refresh::Timeout, but since keyboard is not locked or fastmode=true, return true anyway");

				return true;
			}

			Thread.Sleep(10);
		} while (DateTime.Now.Ticks / 10000 < end);

		if (Audit != null) Audit.WriteLine("Refresh::Timed out (2) waiting for a valid screen. Timeout was " + timeoutMs);

		if (!Config.FastScreenMode && Config.ThrowExceptionOnLockedScreen && KeyboardLocked != 0)
			throw new ApplicationException(
				"Timeout waiting for new screen with keyboard inhibit false - screen present with keyboard inhibit. Turn off the configuration option 'ThrowExceptionOnLockedScreen' to turn off this exception. Timeout was " +
				timeoutMs + " and keyboard inhibit is " + KeyboardLocked);

		if (Config.IdentificationEngineOn) throw new TnIdentificationException(GetScreenAsXml());

		return false;
	}

	/// <summary>
	///     Dump fields to the current audit output
	/// </summary>
	public void ShowFields()
	{
		if (_currentConnection == null) throw new TnHostException("TNEmulator is not connected", "There is no currently open TN3270 connection", null);

		if (Audit != null)
		{
			Audit.WriteLine("-------------------dump screen data -----------------");
			_currentConnection.ExecuteAction(false, "Fields");
			Audit.WriteLine("" + _currentConnection.GetAllStringData());
			CurrentScreen.Dump(Audit);
			Audit.WriteLine("-------------------dump screen end -----------------");
		}
		else
		{
			throw new ApplicationException("ShowFields requires an active 'Audit' connection on the emulator");
		}
	}

	/// <summary>
	///     Retrieves text at the specified location on the screen
	/// </summary>
	/// <param name="x">Column</param>
	/// <param name="y">Row</param>
	/// <param name="length">Length of the text to be returned</param>
	/// <returns></returns>
	public string GetText(int x, int y, int length)
	{
		return CurrentScreen.GetText(x, y, length);
	}

	/// <summary>
	///     Sends a string starting at the indicated screen position
	/// </summary>
	/// <param name="text">The text to send</param>
	/// <param name="x">Column</param>
	/// <param name="y">Row</param>
	/// <returns>True for success</returns>
	public bool SetText(string text, int x, int y)
	{
		if (_currentConnection == null) throw new TnHostException("TNEmulator is not connected", "There is no currently open TN3270 connection", null);

		SetCursor(x, y);

		return SetText(text);
	}

	/// <summary>
	///     Sents the specified string to the emulator at it's current position.
	/// </summary>
	/// <param name="text"></param>
	/// <returns></returns>
	public bool SetText(string text)
	{
		if (_currentConnection == null) throw new TnHostException("TNEmulator is not connected", "There is no currently open TN3270 connection", null);

		lock (this)
		{
			DisposeOfCurrentScreenXml();
		}

		return _currentConnection.ExecuteAction(false, "String", text);
	}

	/// <summary>
	///     Returns after new screen data has stopped flowing from the host for screenCheckInterval time.
	/// </summary>
	/// <param name="screenCheckInterval">
	///     The amount of time between screen data comparisons in milliseconds.
	///     It's probably impractical for this to be much less than 100 ms.
	/// </param>
	/// <param name="finalTimeout">The absolute longest time we should wait before the method should time out</param>
	/// <returns>True if data ceased, and false if the operation timed out. </returns>
	public bool WaitForHostSettle(int screenCheckInterval, int finalTimeout)
	{
		var success = true;
		//Accumulator for total poll time.  This is less accurate than using an interrupt or DateTime deltas, but it's light weight.
		var elapsed = 0;

		//This is low tech and slow, but simple to implement right now.
		while (!Refresh(true, screenCheckInterval))
		{
			if (elapsed > finalTimeout)
			{
				success = false;
				break;
			}

			elapsed += screenCheckInterval;
		}

		return success;
	}

	/// <summary>
	///     Returns the last asynchronous error that occured internally
	/// </summary>
	/// <returns></returns>
	public string GetLastError()
	{
		if (_currentConnection == null) throw new TnHostException("TNEmulator is not connected", "There is no currently open TN3270 connection", null);
		return _currentConnection.LastException;
	}

	/// <summary>
	///     Set field value.
	/// </summary>
	/// <param name="index"></param>
	/// <param name="text"></param>
	public void SetField(int index, string text)
	{
		if (_currentConnection == null) throw new TnHostException("TNEmulator is not connected", "There is no currently open TN3270 connection", null);
		if (index == -1001)
			switch (text)
			{
				case "showparseerror":
					_currentConnection.ShowParseError = true;
					return;
				default:
					return;
			}

		_currentConnection.ExecuteAction(false, "FieldSet", index, text);
		DisposeOfCurrentScreenXml();
	}

	/// <summary>
	///     Set the cursor position on the screen
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	public void SetCursor(int x, int y)
	{
		if (_currentConnection == null) throw new TnHostException("TNEmulator is not connected", "There is no currently open TN3270 connection", null);
		//currentConnection.ExecuteAction("MoveCursor", x,y);
		_currentConnection.MoveCursor(CursorOp.Exact, x, y);
	}

	/// <summary>
	///     Connects to the mainframe.
	/// </summary>
	public void Connect()
	{
		Connect(Config.HostName,
			Config.HostPort,
			Config.HostLu);
	}

	/// <summary>
	///     Connects to host using a local IP endpoint
	///     <remarks>
	///         Added by CFCJR on Feb/29/2008
	///         if a source IP is given then use it for the local IP
	///     </remarks>
	/// </summary>
	/// <param name="localIp"></param>
	/// <param name="host"></param>
	/// <param name="port"></param>
	public void Connect(string localIp, string host, int port)
	{
		LocalIp = localIp;
		Connect(host, port, string.Empty);
	}

	/// <summary>
	///     Connect to TN3270 server using the connection details specified.
	/// </summary>
	/// <remarks>
	///     You should set the Audit property to an instance of an object that implements
	///     the IAudit interface if you want to see any debugging information from this function
	///     call.
	/// </remarks>
	/// <param name="host">Host name or IP address. Mandatory</param>
	/// <param name="port">TCP/IP port to connect to (default TN3270 port is 23)</param>
	/// <param name="lu">TN3270E LU to connect to. Specify null for no LU.</param>
	public void Connect(string host, int port, string lu)
	{
		if (_currentConnection != null)
		{
			_currentConnection.Disconnect();
			_currentConnection.CursorLocationChanged -= currentConnection_CursorLocationChanged;
		}

		try
		{
			_semaphore.Reset();

			_currentConnection = null;
			_currentConnection = new Tn3270Api();
			_currentConnection.Debug = Debug;
			_currentConnection.RunScriptRequested += currentConnection_RunScriptEvent;
			_currentConnection.CursorLocationChanged += currentConnection_CursorLocationChanged;
			_currentConnection.Disconnected += _apiOnDisconnectDelegate;

			_apiOnDisconnectDelegate = currentConnection_OnDisconnect;

			//
			// Debug out our current state
			//
			if (Audit != null)
			{
				Audit.WriteLine("Open3270 emulator version " + Assembly.GetAssembly(typeof(TnEmulator))?.GetName().Version);

				if (_firstTime) _firstTime = false;
				if (Debug)
				{
					Config.Dump(Audit);
					Audit.WriteLine("Connect to host \"" + host + "\"");
					Audit.WriteLine("           port \"" + port + "\"");
					Audit.WriteLine("           LU   \"" + lu + "\"");
					Audit.WriteLine("     Local IP   \"" + LocalIp + "\"");
				}
			}

			_currentConnection.UseSsl = UseSsl;


			if (!string.IsNullOrEmpty(LocalIp))
				_currentConnection.Connect(Audit, LocalIp, host, port, Config);
			else
				_currentConnection.Connect(Audit, host, port, lu, Config);

			_currentConnection.WaitForConnect(-1);
			DisposeOfCurrentScreenXml();

			// Force refresh 
			// GetScreenAsXML();
		}
		catch (Exception)
		{
			_currentConnection = null;
			throw;
		}

		// These don't close the connection
		Refresh(true, 10000);
		if (Audit != null && Debug) Audit.WriteLine("Debug::Connected");
		//mScreenProcessor.Update_Screen(currentScreenXML, true);
	}

	/// <summary>
	///     Closes the current connection to the mainframe.
	/// </summary>
	public void Close()
	{
		if (_currentConnection != null)
		{
			_currentConnection.Disconnect();
			_currentConnection = null;
		}
	}

	/// <summary>
	///     Waits for the specified text to appear at the specified location.
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="text"></param>
	/// <param name="timeoutMs"></param>
	/// <returns></returns>
	public bool WaitForText(int x, int y, string text, int timeoutMs)
	{
		if (_currentConnection == null) throw new TnHostException("TNEmulator is not connected", "There is no currently open TN3270 connection", null);
		var start = DateTime.Now.Ticks;
		//bool ok = false;
		if (Config.AlwaysRefreshWhenWaiting)
			lock (this)
			{
				DisposeOfCurrentScreenXml();
			}

		do
		{
			if (CurrentScreen != null)
			{
				var screenText = CurrentScreen.GetText(x, y, text.Length);
				if (screenText == text)
				{
					if (Audit != null)
						Audit.WriteLine("WaitForText('" + text + "') Found!");
					return true;
				}
			}

			//
			if (timeoutMs == 0)
			{
				if (Audit != null)
					Audit.WriteLine("WaitForText('" + text + "') Not found");
				return false;
			}

			//
			Thread.Sleep(100);
			if (Config.AlwaysRefreshWhenWaiting)
				lock (this)
				{
					DisposeOfCurrentScreenXml();
				}

			Refresh(true, 1000);
		} while ((DateTime.Now.Ticks - start) / 10000 < timeoutMs);

		//
		if (Audit != null)
			Audit.WriteLine("WaitForText('" + text + "') Timed out");
		return false;
	}

	/// <summary>
	///     Waits for the specified text to appear at the specified location.
	/// </summary>
	/// <param name="timeoutMs"></param>
	/// <param name="text"></param>
	/// <returns>StreingPosition structure</returns>
	public StringPosition WaitForTextOnScreen2(int timeoutMs, params string[] text)
	{
		if (WaitForTextOnScreen(timeoutMs, text) != -1)
			return CurrentScreen.LookForTextStrings2(text);
		return null;
	}


	/// <summary>
	///     Waits for the specified text to appear at the specified location.
	/// </summary>
	/// <param name="timeoutMs"></param>
	/// <param name="text"></param>
	/// <returns>Index of text on screen, -1 if not found or timeout occurs</returns>
	public int WaitForTextOnScreen(int timeoutMs, params string[] text)
	{
		if (_currentConnection == null) throw new TnHostException("TNEmulator is not connected", "There is no currently open TN3270 connection", null);
		var start = DateTime.Now.Ticks;
		//bool ok = false;
		if (Config.AlwaysRefreshWhenWaiting)
			lock (this)
			{
				DisposeOfCurrentScreenXml();
			}

		do
		{
			lock (this)
			{
				if (CurrentScreen != null)
				{
					var index = CurrentScreen.LookForTextStrings(text);
					if (index != -1)
					{
						if (Audit != null)
							Audit.WriteLine("WaitForText('" + text[index] + "') Found!");
						return index;
					}
				}
			}

			//
			if (timeoutMs > 0)
			{
				//
				Thread.Sleep(100);
				if (Config.AlwaysRefreshWhenWaiting)
					lock (this)
					{
						DisposeOfCurrentScreenXml();
					}

				Refresh(true, 1000);
			}
		} while (timeoutMs > 0 && (DateTime.Now.Ticks - start) / 10000 < timeoutMs);

		//
		if (Audit != null)
		{
			var temp = text.Aggregate("", (current, _) => current + "t" + "//");

			Audit.WriteLine("WaitForText('" + temp + "') Timed out");
		}

		return -1;
	}

	/// <summary>
	///     Dump current screen to the current audit output
	/// </summary>
	public void Dump(bool withCoordinates = false)
	{
		if (Audit == null) return;
		lock (this)
		{
			CurrentScreen.Dump(Audit, withCoordinates);
		}
	}

	/// <summary>
	///     Refreshes the connection to the mainframe.
	/// </summary>
	public void Refresh()
	{
		lock (this)
		{
			DisposeOfCurrentScreenXml();
		}
	}

	#endregion

	#region Protected / Internal Methods



	protected virtual void Dispose(bool disposing)
	{
		lock (this)
		{
			if (IsDisposed)
				return;
			IsDisposed = true;

			if (Audit != null && Debug)
				Audit.WriteLine("TNEmulator.Dispose(" + IsDisposed + ")");

			if (disposing)
			{
				//----------------------------
				// release managed resources

				if (_currentConnection != null)
				{
					if (Audit != null && Debug)
						Audit.WriteLine("TNEmulator.Dispose() Disposing of currentConnection");
					try
					{
						_currentConnection.Disconnect();
						_currentConnection.CursorLocationChanged -= currentConnection_CursorLocationChanged;

						if (_apiOnDisconnectDelegate != null)
							_currentConnection.Disconnected -= _apiOnDisconnectDelegate;

						_currentConnection.Dispose();
					}
					catch
					{
						if (Audit != null && Debug)
							Audit.WriteLine("TNEmulator.Dispose() Exception during currentConnection.Dispose");
					}

					_currentConnection = null;
				}

				Disconnected = null;

				if (Audit != null && Debug)
					Audit.WriteLine("TNEmulator.Dispose() Disposing of currentScreenXML");

				DisposeOfCurrentScreenXml();
				ObjectState = null;
			}

			//------------------------------
			// release unmanaged resources
		}
	}

	protected virtual void OnCursorLocationChanged(EventArgs args)
	{
		CursorLocationChanged?.Invoke(this, args);
	}

	/// <summary>
	///     Get the current screen as an XMLScreen data structure
	/// </summary>
	/// <returns></returns>
	internal IScreen GetScreenAsXml()
	{
		DisposeOfCurrentScreenXml();

		if (_currentConnection == null) throw new TnHostException("TNEmulator is not connected", "There is no currently open TN3270 connection", null);
		if (_currentConnection.ExecuteAction(false, "DumpXML"))
			//
			return Screen.LoadFromString(_currentConnection.GetAllStringData());

		return null;
	}

	#endregion

	#region Private Methods

	private void currentConnection_CursorLocationChanged(object sender, EventArgs e)
	{
		OnCursorLocationChanged(e);
	}

	private void currentConnection_RunScriptEvent(string where)
	{
		lock (this)
		{
			DisposeOfCurrentScreenXml();

			if (Audit != null && Debug) Audit.WriteLine("mre.Release(1) from location " + where);
			_semaphore.Release();
		}
	}

	/// <summary>
	///     Wait for some text to appear at the specified location
	/// </summary>
	/// <returns></returns>
	private void currentConnection_OnDisconnect(TnEmulator where, string reason)
	{
		Disconnected?.Invoke(this, reason);
	}

	#endregion

	public bool SendText(string text)
	{
		if (_currentConnection == null) throw new TnHostException("TNEmulator is not connected", "There is no currently open TN3270 connection", null);
		lock (this)
		{
			DisposeOfCurrentScreenXml();
		}

		return _currentConnection.ExecuteAction(false, "String", text);
	}
}

public delegate void OnDisconnectDelegate(TnEmulator where, string reason);
