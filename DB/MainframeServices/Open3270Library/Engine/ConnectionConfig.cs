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
using System.IO;

namespace Open3270
{
	/// <summary>
	/// Connection configuration class holds the configuration options for a connection
	/// </summary>
	public class ConnectionConfig
	{
		internal void Dump(IAudit sout)
		{
			if (sout == null) return;
			sout.WriteLine("Config.FastScreenMode " + FastScreenMode);
			sout.WriteLine("Config.IgnoreSequenceCount " + IgnoreSequenceCount);
			sout.WriteLine("Config.IdentificationEngineOn " + IdentificationEngineOn);
			sout.WriteLine("Config.AlwaysSkipToUnprotected " + AlwaysSkipToUnprotected);
			sout.WriteLine("Config.LockScreenOnWriteToUnprotected " + LockScreenOnWriteToUnprotected);
			sout.WriteLine("Config.ThrowExceptionOnLockedScreen " + ThrowExceptionOnLockedScreen);
			sout.WriteLine("Config.DefaultTimeout " + DefaultTimeout);
			sout.WriteLine("Config.hostName " + HostName);
			sout.WriteLine("Config.hostPort " + HostPort);
			sout.WriteLine("Config.hostLU " + HostLu);
			sout.WriteLine("Config.termType " + TermType);
			sout.WriteLine("Config.AlwaysRefreshWhenWaiting " + AlwaysRefreshWhenWaiting);
			sout.WriteLine("Config.SubmitAllKeyboardCommands " + SubmitAllKeyboardCommands);
			sout.WriteLine("Config.RefuseTN3270E " + RefuseTn3270E);
			sout.WriteLine("Config.KeepAlivePeriod " + KeepAlivePeriod);
		}

		/// <summary>
		/// The host name to connect to
		/// </summary>
		public string HostName { get; set; } = null;

		/// <summary>
		/// Host Port
		/// </summary>
		public int HostPort { get; set; } = 23;

		/// <summary>
		/// Host LU, null for none
		/// </summary>
		public string HostLu { get; set; } = null;

		/// <summary>
		/// Terminal type for host
		/// </summary>
		public string TermType { get; set; } = null;

		public bool UseSsl { get; set; }

		/// <summary>
		/// Is the internal screen identification engine turned on? Default false.
		/// </summary>
		public bool IdentificationEngineOn { get; set; }

		/// <summary>
		/// Should we skip to the next unprotected field if SendText is called
		/// on an protected field? Default true.
		/// </summary>
		public bool AlwaysSkipToUnprotected { get; set; } = true;

		/// <summary>
		/// Lock the screen if user tries to write to a protected field. Default false.
		/// </summary>
		public bool LockScreenOnWriteToUnprotected { get; set; }

		/// <summary>
		/// Default timeout for operations such as SendKeyFromText. Default value is 40000 (40 seconds).
		/// </summary>
		public TimeSpan DefaultTimeout { get; set; } 

		/// <summary>
		/// Flag to set whether an exception should be thrown if a screen write met
		/// a locked screen. Default is now true.
		/// </summary>
		public bool ThrowExceptionOnLockedScreen { get; set; } = true;

		/// <summary>
		/// Whether to ignore host request for sequence counting
		/// </summary>
		public bool IgnoreSequenceCount { get; set; }

		/// <summary>
		/// Allows connection to be connected to a proxy log file rather than directly to a host
		/// for debugging.
		/// </summary>
		public StreamReader LogFile { get; set; }

		/// <summary>
		/// Whether to ignore keyboard inhibit when moving between screens. Significantly speeds up operations, 
		/// but can result in locked screens and data loss if you try to key data onto a screen that is still locked.
		/// </summary>
		public bool FastScreenMode { get; set; }


		/// <summary>
		/// Whether the screen should always be refreshed when waiting for an update. Default is false.
		/// </summary>
		public bool AlwaysRefreshWhenWaiting { get; set; }


		/// <summary>
		/// Whether to refresh the screen for keys like TAB, BACKSPACE etc should refresh the host. Default is now false.
		/// </summary>
		public bool SubmitAllKeyboardCommands { get; set; }

		/// <summary>
		/// Whether to refuse a TN3270E request from the host, despite the terminal type
		/// </summary>
		public bool RefuseTn3270E { get; set; }

		/// <summary>
		/// The frequency the NOP Command is Sent
		/// </summary>
		public TimeSpan? KeepAlivePeriod { get; set; } = TimeSpan.FromMinutes(2);
	}
}
