#region License

/*
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
using System.Threading;

namespace Open3270.TN3270;

/// <summary>
///     Summary description for Idle.
/// </summary>
internal class Idle : IDisposable
{
	// 7 minutes
	private const int IdleMilliseconds = 7 * 60 * 1000;


	private Timer idleTimer;

	private bool idleWasIn3270;
	private bool isTicking;

	private int milliseconds;
	private Random rand;
	private bool randomize;
	private readonly Telnet telnet;

	internal Idle(Telnet tn)
	{
		telnet = tn;
	}


	public void Dispose()
	{
		if (telnet != null) telnet.Connected3270 -= telnet_Connected3270;
	}

	// Initialization
	private void Initialize()
	{
		// Register for state changes.
		telnet.Connected3270 += telnet_Connected3270;

		// Seed the random number generator (we seem to be the only user).
		rand = new Random();
	}

	private void telnet_Connected3270(object sender, Connected3270EventArgs e)
	{
		IdleIn3270(e.Is3270);
	}

	/// <summary>
	///     Process a timeout value.
	/// </summary>
	/// <param name="t"></param>
	/// <returns></returns>
	private int ProcessTimeoutValue(string t)
	{
		if (t == null || t.Length == 0)
		{
			milliseconds = IdleMilliseconds;
			randomize = true;
			return 0;
		}

		if (t[0] == '~')
		{
			randomize = true;
			t = t.Substring(1);
		}

		throw new ApplicationException("process_timeout_value not implemented");
	}


	/// <summary>
	///     Called when a host connects or disconnects.
	/// </summary>
	/// <param name="in3270"></param>
	private void IdleIn3270(bool in3270)
	{
		if (in3270 && !idleWasIn3270)
		{
			idleWasIn3270 = true;
		}
		else
		{
			if (isTicking)
			{
				telnet.Controller.RemoveTimeOut(idleTimer);
				isTicking = false;
			}

			idleWasIn3270 = false;
		}
	}


	private void TimedOut(object state)
	{
		lock (telnet)
		{
			telnet.Trace.trace_event("Idle timeout\n");
			//Console.WriteLine("PUSH MACRO ignored (BUGBUG)");
			//push_macro(idle_command, false);
			ResetIdleTimer();
		}
	}


	/// <summary>
	///     Reset (and re-enable) the idle timer.  Called when the user presses an AID key.
	/// </summary>
	public void ResetIdleTimer()
	{
		if (milliseconds != 0)
		{
			int idleMsNow;

			if (isTicking)
			{
				telnet.Controller.RemoveTimeOut(idleTimer);
				isTicking = false;
			}

			idleMsNow = milliseconds;

			if (randomize)
			{
				idleMsNow = milliseconds;
				if (rand.Next(100) % 2 != 0)
					idleMsNow += rand.Next(milliseconds / 10);
				else
					idleMsNow -= rand.Next(milliseconds / 10);
			}

			telnet.Trace.trace_event("Setting idle timeout to " + idleMsNow);
			idleTimer = telnet.Controller.AddTimeout(idleMsNow, TimedOut);
			isTicking = true;
		}
	}
}
