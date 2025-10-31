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

internal class Appres
{
	public const int MonoCase = 0;
	public const int AltCursor = 1;
	public const int CursorBlink = 2;
	public const int ShowTiming = 3;
	public const int CursorPos = 4;
	public const int DsTrace = 5;
	public const int ScrollBar = 6;
	public const int LINE_WRAP = 7;
	public const int BlankFill = 8;
	public const int ScreenTrace = 9;
	public const int EventTrace = 10;
	public const int MarginedPaste = 11;
	public const int RectangleSelect = 12;

	private const int N_TOGGLES = 14;

	//public bool	once;
	public bool AplMode;

	public string Charset;

	//public bool highlight_bold;
	public bool Color8 = false;
	public bool DebugTracing;
	public bool DisconnectClear = false;
	public string Eof;
	public string Erase;
	public bool Extended;

	/* Named resources */
	//public string conf_dir;
	//public string model;
	public string Hostsfile;
	//public string connectfile_name;
	//public string idle_command;
	//public bool idle_command_enabled;
	//public string idle_timeout;


	/* Line-mode TTY parameters */
	public bool Icrnl;
	public bool Inlcr;
	public string Intr;
	public string Kill;
	public string Lnext;
	public bool M3279;
	public string Macros;
	public bool ModifiedSel;

	/* Application resources */
	/* Options (not toggles) */
	public bool Mono;
	public bool NumericLock;

	public bool Onlcr;

	//public string trace_file;
	//public string screentrace_file;
	//public string trace_file_size;
	public string Oversize;
	public string Port;
	public string Quit;
	public string Rprnt;
	public bool Scripted;
	public bool Secure;
	public string Termname;


	private readonly Toggle[] _toggles;

	public string TraceDir;

	//public bool oerr_lock;
	public bool Typeahead;
	public string Werase;

	public Appres()
	{
		_toggles = new Toggle[N_TOGGLES];
		int i;
		for (i = 0; i < N_TOGGLES; i++) _toggles[i] = new Toggle();
		// Set defaults
		Mono = false;
		Extended = true;
		M3279 = false;
		ModifiedSel = false;
		AplMode = false;
		Scripted = true;
		NumericLock = false;
		Secure = false;
		//this.oerr_lock = false;
		Typeahead = true;
		DebugTracing = true;

		//this.model = "4";
		Hostsfile = null;
		Port = "telnet";
		Charset = "bracket";
		Termname = null;
		Macros = null;
		TraceDir = "/tmp";
		Oversize = null;

		Icrnl = true;
		Inlcr = false;
		Onlcr = true;
		Erase = "^H";
		Kill = "^U";
		Werase = "^W";
		Rprnt = "^R";
		Lnext = "^V";
		Intr = "^C";
		Quit = "^\\";
		Eof = "^D";
	}

	public bool Toggled(int ix)
	{
		return _toggles[ix].ToggleValue;
	}

	public void ToggleTheValue(Toggle t)
	{
		t.ToggleValue = !t.ToggleValue;
		t.Changed = true;
	}

	public void ToggleTheValue(int ix)
	{
		_toggles[ix].ToggleValue = !_toggles[ix].ToggleValue;
		_toggles[ix].Changed = true;
	}

	public void SetToggle(int ix, bool v)
	{
		_toggles[ix].ToggleValue = v;
		_toggles[ix].Changed = true;
	}

	public bool ToggleAction(params object[] args)
	{
		throw new ApplicationException("toggle_action not implemented");
	}
	
	internal class Toggle
	{
		// Has the value changed since init 
		public bool Changed;
		public readonly string[] Labels;
		public bool ToggleValue;

		internal Toggle()
		{
			ToggleValue = false;
			Changed = false;
			Labels = new string[2];
			Labels[0] = null;
			Labels[1] = null;
		}
	}
}
