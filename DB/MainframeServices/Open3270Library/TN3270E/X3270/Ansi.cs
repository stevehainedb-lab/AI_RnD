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

internal enum AnsiState
{
	DATA = 0,
	ESC = 1,
	CSDES = 2,
	N1 = 3,
	DECP = 4,
	TEXT = 5,
	TEXT2 = 6
}

internal delegate AnsiState AnsiDelegate(int ig1, int ig2);

/// <summary>
///     Summary description for ansi.
/// </summary>
internal class Ansi : IDisposable
{
	public const int CS_G0 = 0;
	public const int CS_G1 = 1;
	public const int CS_G2 = 2;
	public const int CS_G3 = 3;


	public const int CSD_LD = 0;
	public const int CSD_UK = 1;
	public const int CSD_US = 2;
	public const int DEFAULT_CGEN = 0x02b90000;
	public const int DEFAULT_CSET = 0x00000025;

	public const byte SC = 1; /* save cursor position */
	public const byte RC = 2; /* restore cursor position */
	public const byte NL = 3; /* new line */
	public const byte UP = 4; /* cursor up */
	public const byte E2 = 5; /* second level of ESC processing */
	public const byte RS = 6; /* reset */
	public const byte IC = 7; /* insert chars */
	public const byte DN = 8; /* cursor down */
	public const byte RT = 9; /* cursor right */
	public const byte LT = 10; /* cursor left */
	public const byte CM = 11; /* cursor motion */
	public const byte ED = 12; /* erase in display */
	public const byte EL = 13; /* erase in line */
	public const byte IL = 14; /* insert lines */
	public const byte DL = 15; /* delete lines */
	public const byte DC = 16; /* delete characters */
	public const byte SG = 17; /* set graphic rendition */
	public const byte BL = 18; /* ring bell */
	public const byte NP = 19; /* new page */
	public const byte BS = 20; /* backspace */
	public const byte CR = 21; /* carriage return */
	public const byte LF = 22; /* line feed */
	public const byte HT = 23; /* horizontal tab */
	public const byte E1 = 24; /* first level of ESC processing */
	public const byte XX = 25; /* undefined control character (nop) */
	public const byte PC = 26; /* printing character */
	public const byte Sc = 27; /* semicolon (after ESC [) */
	public const byte DG = 28; /* digit (after ESC [ or ESC [ ?) */
	public const byte RI = 29; /* reverse index */
	public const byte DA = 30; /* send device attributes */
	public const byte SM = 31; /* set mode */
	public const byte RM = 32; /* reset mode */
	public const byte DO = 33; /* return terminal ID (obsolete) */
	public const byte SR = 34; /* device status report */
	public const byte CS = 35; /* character set designate */
	public const byte E3 = 36; /* third level of ESC processing */
	public const byte DS = 37; /* DEC private set */
	public const byte DR = 38; /* DEC private reset */
	public const byte DV = 39; /* DEC private save */
	public const byte DT = 40; /* DEC private restore */
	public const byte SS = 41; /* set scrolling region */
	public const byte TM = 42; /* text mode (ESC ]) */
	public const byte T2 = 43; /* semicolon (after ESC ]) */
	public const byte TX = 44; /* text parameter (after ESC ] n ;) */
	public const byte TB = 45; /* text parameter done (ESC ] n ; xxx BEL) */
	public const byte TS = 46; /* tab set */
	public const byte TC = 47; /* tab clear */
	public const byte C2 = 48; /* character set designate (finish) */
	public const byte G0 = 49; /* select G0 character set */
	public const byte G1 = 50; /* select G1 character set */
	public const byte G2 = 51; /* select G2 character set */
	public const byte G3 = 52; /* select G3 character set */
	public const byte S2 = 53; /* select G2 for next character */
	public const byte S3 = 54; /* select G3 for next character */
	public const int NN = 20;
	public const int NT = 256;

	private AnsiDelegate[] _ansiFn;

	private bool _ansiResetFirst;

	private readonly object[] _st = new object[7];
	private readonly Telnet _telnet;
	public bool AllowWideMode;
	public char AnsiCh;
	public bool AnsiInsertMode;
	public int ApplCursor;
	public bool AutoNewlineMode;
	public byte Bg;
	public int[] Csd = new[] { CSD_US, CSD_US, CSD_US, CSD_US };
	public int Cset = CS_G0;
	public string Csnames = "0AB";
	public int CsToChange;
	public byte Fg;
	public string Gnnames = "()*+";
	public byte Gr;
	public bool HeldWrap;
	public int[] N = new int[NN];
	public int Nx;
	public int OnceCset = -1;
	public bool RevWraparoundMode;
	public bool SavedAllowWideMode;
	public bool SavedAltbuffer;
	public int SavedApplCursor;
	public byte SavedBg;
	public int[] SavedCsd = new[] { CSD_US, CSD_US, CSD_US, CSD_US };
	public int SavedCset = CS_G0;


	public int SavedCursor;
	public byte SavedFg;
	public byte SavedGr;
	public bool SavedRevWraparoundMode;
	public bool SavedWideMode;
	public bool SavedWraparoundMode = true;

	public int ScrollBottom = -1;
	public int ScrollTop = -1;

	public AnsiState State;
	public byte[] Tabs;
	public string Text; //char     text[NT + 1];
	public int Tx;
	public bool WideMode;
	public bool WraparoundMode = true;

	internal Ansi(Telnet telnet)
	{
		_telnet = telnet;
		Initialize_ansi_fn();
		InitializeSt();
	}

	public void Dispose()
	{
		_telnet.Connected3270 -= telnet_Connected3270;
	}

	private void Initialize_ansi_fn()
	{
		_ansiFn =
		[
			/* 0 */ ansi_data_mode,
			/* 1 */ dec_save_cursor,
			/* 2 */ dec_restore_cursor,
			/* 3 */ ansi_newline,
			/* 4 */ ansi_cursor_up,
			/* 5 */ ansi_esc2,
			/* 6 */ ansi_reset,
			/* 7 */ ansi_insert_chars,
			/* 8 */ ansi_cursor_down,
			/* 9 */ ansi_cursor_right,
			/* 10 */ ansi_cursor_left,
			/* 11 */ ansi_cursor_motion,
			/* 12 */ ansi_erase_in_display,
			/* 13 */ ansi_erase_in_line,
			/* 14 */ ansi_insert_lines,
			/* 15 */ ansi_delete_lines,
			/* 16 */ ansi_delete_chars,
			/* 17 */ ansi_sgr,
			/* 18 */ ansi_bell,
			/* 19 */ ansi_newpage,
			/* 20 */ ansi_backspace,
			/* 21 */ ansi_cr,
			/* 22 */ ansi_lf,
			/* 23 */ ansi_htab,
			/* 24 */ ansi_escape,
			/* 25 */ ansi_nop,
			/* 26 */ ansi_printing,
			/* 27 */ ansi_semicolon,
			/* 28 */ ansi_digit,
			/* 29 */ ansi_reverse_index,
			/* 30 */ ansi_send_attributes,
			/* 31 */ ansi_set_mode,
			/* 32 */ ansi_reset_mode,
			/* 33 */ dec_return_terminal_id,
			/* 34 */ ansi_status_report,
			/* 35 */ ansi_cs_designate,
			/* 36 */ ansi_esc3,
			/* 37 */ dec_set,
			/* 38 */ dec_reset,
			/* 39 */ dec_save,
			/* 40 */ dec_restore,
			/* 41 */ dec_scrolling_region,
			/* 42 */ xterm_text_mode,
			/* 43 */ xterm_text_semicolon,
			/* 44 */ xterm_text,
			/* 45 */ xterm_text_do,
			/* 46 */ ansi_htab_set,
			/* 47 */ ansi_htab_clear,
			/* 48 */ ansi_cs_designate2,
			/* 49 */ ansi_select_g0,
			/* 50 */ ansi_select_g1,
			/* 51 */ ansi_select_g2,
			/* 52 */ ansi_select_g3,
			/* 53 */ ansi_one_g2,
			/* 54 */ ansi_one_g3
		];
	}

	///*vok*/static byte st[7][256] = {
	private void InitializeSt()
	{
		/*
		 * State table for base processing (state == DATA)
		 */
		_st[0] = new[]
		{
			/*       0  1  2  3  4  5  6  7  8  9  a  b  c  d  e  f  */
			/* 00 */ XX, XX, XX, XX, XX, XX, XX, BL, BS, HT, LF, LF, NP, CR, G1, G0,
			/* 10 */ XX, XX, XX, XX, XX, XX, XX, XX, XX, XX, XX, E1, XX, XX, XX, XX,
			/* 20 */ PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC,
			/* 30 */ PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC,
			/* 40 */ PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC,
			/* 50 */ PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC,
			/* 60 */ PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC,
			/* 70 */ PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, XX,
			/* 80 */ XX, XX, XX, XX, XX, XX, XX, XX, XX, XX, XX, XX, XX, XX, XX, XX,
			/* 90 */ XX, XX, XX, XX, XX, XX, XX, XX, XX, XX, XX, XX, XX, XX, XX, XX,
			/* a0 */ PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC,
			/* b0 */ PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC,
			/* c0 */ PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC,
			/* d0 */ PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC,
			/* e0 */ PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC,
			/* f0 */ PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC, PC
		};

		/*
		 * State table for ESC processing (state == ESC)
		 */
		_st[1] = new byte[]
		{
			/* 0  1  2  3  4  5  6  7  8  9  a  b  c  d  e  f  */
			/* 00 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* 10 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* 20 */ 0, 0, 0, 0, 0, 0, 0, 0, CS, CS, CS, CS, 0, 0, 0, 0,
			/* 30 */ 0, 0, 0, 0, 0, 0, 0, SC, RC, 0, 0, 0, 0, 0, 0, 0,
			/* 40 */ 0, 0, 0, 0, 0, NL, 0, 0, TS, 0, 0, 0, 0, RI, S2, S3,
			/* 50 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, E2, 0, TM, 0, 0,
			/* 60 */ 0, 0, 0, RS, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, G2, G3,
			/* 70 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* 80 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* 90 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* a0 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* b0 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* c0 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* d0 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* e0 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* f0 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
		};

		/*
		 * State table for ESC ()*+ C processing (state == CSDES)
		 */
		_st[2] = new byte[]
		{
			/* 0  1  2  3  4  5  6  7  8  9  a  b  c  d  e  f  */
			/* 00 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* 10 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* 20 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* 30 */ C2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* 40 */ 0, C2, C2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* 50 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* 60 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* 70 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* 80 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* 90 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* a0 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* b0 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* c0 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* d0 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* e0 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* f0 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
		};

		/*
		 * State table for ESC [ processing (state == N1)
		 */
		_st[3] = new byte[]
		{
			/* 0  1  2  3  4  5  6  7  8  9  a  b  c  d  e  f  */
			/* 00 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* 10 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* 20 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* 30 */ DG, DG, DG, DG, DG, DG, DG, DG, DG, DG, 0, Sc, 0, 0, 0, E3,
			/* 40 */ IC, UP, DN, RT, LT, 0, 0, 0, CM, 0, ED, EL, IL, DL, 0, 0,
			/* 50 */ DC, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* 60 */ 0, 0, 0, DA, 0, 0, CM, TC, SM, 0, 0, 0, RM, SG, SR, 0,
			/* 70 */ 0, 0, SS, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* 80 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* 90 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* a0 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* b0 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* c0 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* d0 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* e0 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* f0 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
		};

		/*
		 * State table for ESC [ ? processing (state == DECP)
		 */
		_st[4] = new byte[]
		{
			/* 0  1  2  3  4  5  6  7  8  9  a  b  c  d  e  f  */
			/* 00 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* 10 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* 20 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* 30 */ DG, DG, DG, DG, DG, DG, DG, DG, DG, DG, 0, 0, 0, 0, 0, 0,
			/* 40 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* 50 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* 60 */ 0, 0, 0, 0, 0, 0, 0, 0, DS, 0, 0, 0, DR, 0, 0, 0,
			/* 70 */ 0, 0, DT, DV, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* 80 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* 90 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* a0 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* b0 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* c0 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* d0 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* e0 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* f0 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
		};

		/*
		 * State table for ESC ] processing (state == TEXT)
		 */
		_st[5] = new byte[]
		{
			/* 0  1  2  3  4  5  6  7  8  9  a  b  c  d  e  f  */
			/* 00 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* 10 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* 20 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* 30 */ DG, DG, DG, DG, DG, DG, DG, DG, DG, DG, 0, T2, 0, 0, 0, 0,
			/* 40 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* 50 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* 60 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* 70 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* 80 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* 90 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* a0 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* b0 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* c0 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* d0 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* e0 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* f0 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
		};

		/*
		 * State table for ESC ] n ; processing (state == TEXT2)
		 */
		_st[6] = new byte[]
		{
			/* 0  1  2  3  4  5  6  7  8  9  a  b  c  d  e  f  */
			/* 00 */ 0, 0, 0, 0, 0, 0, 0, TB, 0, 0, 0, 0, 0, 0, 0, 0,
			/* 10 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/* 20 */ TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX,
			/* 30 */ TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX,
			/* 40 */ TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX,
			/* 50 */ TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX,
			/* 60 */ TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX,
			/* 70 */ TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, XX,
			/* 80 */ TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX,
			/* 90 */ TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX,
			/* a0 */ TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX,
			/* b0 */ TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX,
			/* c0 */ TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX,
			/* d0 */ TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX,
			/* e0 */ TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX,
			/* f0 */ TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX, TX
		};
	}


	//static void	ansi_scroll();

	private AnsiState ansi_data_mode(int ig1, int ig2)
	{
		return AnsiState.DATA;
	}

	private AnsiState dec_save_cursor(int ig1, int ig2)
	{
		int i;

		SavedCursor = _telnet.Controller.CursorAddress;
		SavedCset = Cset;
		for (i = 0; i < 4; i++)
			SavedCsd[i] = Csd[i];
		SavedFg = Fg;
		SavedBg = Bg;
		SavedGr = Gr;
		return AnsiState.DATA;
	}

	private AnsiState dec_restore_cursor(int ig1, int ig2)
	{
		int i;

		Cset = SavedCset;
		for (i = 0; i < 4; i++)
			Csd[i] = SavedCsd[i];
		Fg = SavedFg;
		Bg = SavedBg;
		Gr = SavedGr;
		_telnet.Controller.SetCursorAddress(SavedCursor);
		HeldWrap = false;
		return AnsiState.DATA;
	}

	private AnsiState ansi_newline(int ig1, int ig2)
	{
		int nc;

		_telnet.Controller.SetCursorAddress(_telnet.Controller.CursorAddress - _telnet.Controller.CursorAddress % _telnet.Controller.ColumnCount);
		nc = _telnet.Controller.CursorAddress + _telnet.Controller.ColumnCount;
		if (nc < ScrollBottom * _telnet.Controller.ColumnCount)
			_telnet.Controller.SetCursorAddress(nc);
		else
			ansi_scroll();
		HeldWrap = false;
		return AnsiState.DATA;
	}

	private AnsiState ansi_cursor_up(int nn, int ig2)
	{
		int rr;

		if (nn < 1)
			nn = 1;
		rr = _telnet.Controller.CursorAddress / _telnet.Controller.ColumnCount;
		if (rr - nn < 0)
			_telnet.Controller.SetCursorAddress(_telnet.Controller.CursorAddress % _telnet.Controller.ColumnCount);
		else
			_telnet.Controller.SetCursorAddress(_telnet.Controller.CursorAddress - nn * _telnet.Controller.ColumnCount);
		HeldWrap = false;
		return AnsiState.DATA;
	}

	private AnsiState ansi_esc2(int ig1, int ig2)
	{
		int i;

		for (i = 0; i < NN; i++)
			N[i] = 0;
		Nx = 0;
		return AnsiState.N1;
	}

	private AnsiState ansi_reset(int ig1, int ig2)
	{
		int i;
		//static Boolean first = true;

		Gr = 0;
		SavedGr = 0;
		Fg = 0;
		SavedFg = 0;
		Bg = 0;
		SavedBg = 0;
		Cset = CS_G0;
		SavedCset = CS_G0;
		Csd[0] = Csd[1] = Csd[2] = Csd[3] = CSD_US;
		SavedCsd[0] = SavedCsd[1] = SavedCsd[2] = SavedCsd[3] = CSD_US;
		OnceCset = -1;
		SavedCursor = 0;
		AnsiInsertMode = false;
		AutoNewlineMode = false;
		ApplCursor = 0;
		SavedApplCursor = 0;
		WraparoundMode = true;
		SavedWraparoundMode = true;
		RevWraparoundMode = false;
		SavedRevWraparoundMode = false;
		AllowWideMode = false;
		SavedAllowWideMode = false;
		WideMode = false;
		AllowWideMode = false;
		SavedAltbuffer = false;
		ScrollTop = 1;

		ScrollBottom = _telnet.Controller.RowCount;
		Tabs = new byte[(_telnet.Controller.ColumnCount + 7) / 8];
		//Replace(tabs, (byte *)Malloc((telnet.tnctlr.COLS+7)/8));
		for (i = 0; i < (_telnet.Controller.ColumnCount + 7) / 8; i++)
			Tabs[i] = 0x01;
		HeldWrap = false;
		if (!_ansiResetFirst)
		{
			_telnet.Controller.SwapAltBuffers(true);
			_telnet.Controller.EraseRegion(0, _telnet.Controller.RowCount * _telnet.Controller.ColumnCount, true);
			_telnet.Controller.SwapAltBuffers(false);
			_telnet.Controller.Clear(false);
			//screen_80();
		}

		_ansiResetFirst = false;
		return AnsiState.DATA;
	}

	private AnsiState ansi_insert_chars(int nn, int ig2)
	{
		var cc = _telnet.Controller.CursorAddress % _telnet.Controller.ColumnCount; /* current col */
		var mc = _telnet.Controller.ColumnCount - cc; /* max chars that can be inserted */
		int ns; /* chars that are shifting */

		if (nn < 1)
			nn = 1;
		if (nn > mc)
			nn = mc;

		/* Move the surviving chars right */
		ns = mc - nn;
		if (ns != 0)
			_telnet.Controller.CopyBlock(_telnet.Controller.CursorAddress, _telnet.Controller.CursorAddress + nn, ns, true);

		/* Clear the middle of the line */
		_telnet.Controller.EraseRegion(_telnet.Controller.CursorAddress, nn, true);
		return AnsiState.DATA;
	}

	private AnsiState ansi_cursor_down(int nn, int ig2)
	{
		int rr;

		if (nn < 1)
			nn = 1;
		rr = _telnet.Controller.CursorAddress / _telnet.Controller.ColumnCount;
		if (rr + nn >= _telnet.Controller.RowCount)
			_telnet.Controller.SetCursorAddress((_telnet.Controller.RowCount - 1) * _telnet.Controller.ColumnCount +
			                                    _telnet.Controller.CursorAddress % _telnet.Controller.ColumnCount);
		else
			_telnet.Controller.SetCursorAddress(_telnet.Controller.CursorAddress + nn * _telnet.Controller.ColumnCount);
		HeldWrap = false;
		return AnsiState.DATA;
	}

	private AnsiState ansi_cursor_right(int nn, int ig2)
	{
		int cc;

		if (nn < 1)
			nn = 1;
		cc = _telnet.Controller.CursorAddress % _telnet.Controller.ColumnCount;
		if (cc == _telnet.Controller.ColumnCount - 1)
			return AnsiState.DATA;
		if (cc + nn >= _telnet.Controller.ColumnCount)
			nn = _telnet.Controller.ColumnCount - 1 - cc;
		_telnet.Controller.SetCursorAddress(_telnet.Controller.CursorAddress + nn);
		HeldWrap = false;
		return AnsiState.DATA;
	}

	private AnsiState ansi_cursor_left(int nn, int ig2)
	{
		int cc;

		if (HeldWrap)
		{
			HeldWrap = false;
			return AnsiState.DATA;
		}

		if (nn < 1)
			nn = 1;
		cc = _telnet.Controller.CursorAddress % _telnet.Controller.ColumnCount;
		if (cc == 0)
			return AnsiState.DATA;
		if (nn > cc)
			nn = cc;
		_telnet.Controller.SetCursorAddress(_telnet.Controller.CursorAddress - nn);
		return AnsiState.DATA;
	}

	private AnsiState ansi_cursor_motion(int n1, int n2)
	{
		if (n1 < 1) n1 = 1;
		if (n1 > _telnet.Controller.RowCount) n1 = _telnet.Controller.RowCount;
		if (n2 < 1) n2 = 1;
		if (n2 > _telnet.Controller.ColumnCount) n2 = _telnet.Controller.ColumnCount;
		_telnet.Controller.SetCursorAddress((n1 - 1) * _telnet.Controller.ColumnCount + (n2 - 1));
		HeldWrap = false;
		return AnsiState.DATA;
	}

	private AnsiState ansi_erase_in_display(int nn, int ig2)
	{
		switch (nn)
		{
			case 0: /* below */
				_telnet.Controller.EraseRegion(_telnet.Controller.CursorAddress, _telnet.Controller.RowCount * _telnet.Controller.ColumnCount - _telnet.Controller.CursorAddress,
					true);
				break;
			case 1: /* above */
				_telnet.Controller.EraseRegion(0, _telnet.Controller.CursorAddress + 1, true);
				break;
			case 2: /* all (without moving cursor) */
				if (_telnet.Controller.CursorAddress == 0 && !_telnet.Controller.IsAltBuffer)
				{
					//scroll_save(telnet.tnctlr.ROWS, true);
				}

				_telnet.Controller.EraseRegion(0, _telnet.Controller.RowCount * _telnet.Controller.ColumnCount, true);
				break;
		}

		return AnsiState.DATA;
	}

	private AnsiState ansi_erase_in_line(int nn, int ig2)
	{
		var nc = _telnet.Controller.CursorAddress % _telnet.Controller.ColumnCount;

		switch (nn)
		{
			case 0: /* to right */
				_telnet.Controller.EraseRegion(_telnet.Controller.CursorAddress, _telnet.Controller.ColumnCount - nc, true);
				break;
			case 1: /* to left */
				_telnet.Controller.EraseRegion(_telnet.Controller.CursorAddress - nc, nc + 1, true);
				break;
			case 2: /* all */
				_telnet.Controller.EraseRegion(_telnet.Controller.CursorAddress - nc, _telnet.Controller.ColumnCount, true);
				break;
		}

		return AnsiState.DATA;
	}

	private AnsiState ansi_insert_lines(int nn, int ig2)
	{
		var rr = _telnet.Controller.CursorAddress / _telnet.Controller.ColumnCount; /* current row */
		var mr = ScrollBottom - rr; /* rows left at and below this one */
		int ns; /* rows that are shifting */

		/* If outside of the scrolling region, do nothing */
		if (rr < ScrollTop - 1 || rr >= ScrollBottom)
			return AnsiState.DATA;

		if (nn < 1)
			nn = 1;
		if (nn > mr)
			nn = mr;

		/* Move the victims down */
		ns = mr - nn;
		if (ns != 0)
			_telnet.Controller.CopyBlock(rr * _telnet.Controller.ColumnCount, (rr + nn) * _telnet.Controller.ColumnCount, ns * _telnet.Controller.ColumnCount, true);

		/* Clear the middle of the screen */
		_telnet.Controller.EraseRegion(rr * _telnet.Controller.ColumnCount, nn * _telnet.Controller.ColumnCount, true);
		return AnsiState.DATA;
	}

	private AnsiState ansi_delete_lines(int nn, int ig2)
	{
		var rr = _telnet.Controller.CursorAddress / _telnet.Controller.ColumnCount; /* current row */
		var mr = ScrollBottom - rr; /* max rows that can be deleted */
		int ns; /* rows that are shifting */

		/* If outside of the scrolling region, do nothing */
		if (rr < ScrollTop - 1 || rr >= ScrollBottom)
			return AnsiState.DATA;

		if (nn < 1)
			nn = 1;
		if (nn > mr)
			nn = mr;

		/* Move the surviving rows up */
		ns = mr - nn;
		if (ns != 0)
			_telnet.Controller.CopyBlock((rr + nn) * _telnet.Controller.ColumnCount, rr * _telnet.Controller.ColumnCount, ns * _telnet.Controller.ColumnCount, true);

		/* Clear the rest of the screen */
		_telnet.Controller.EraseRegion((rr + ns) * _telnet.Controller.ColumnCount, nn * _telnet.Controller.ColumnCount, true);
		return AnsiState.DATA;
	}

	private AnsiState ansi_delete_chars(int nn, int ig2)
	{
		var cc = _telnet.Controller.CursorAddress % _telnet.Controller.ColumnCount; /* current col */
		var mc = _telnet.Controller.ColumnCount - cc; /* max chars that can be deleted */
		int ns; /* chars that are shifting */

		if (nn < 1)
			nn = 1;
		if (nn > mc)
			nn = mc;

		/* Move the surviving chars left */
		ns = mc - nn;
		if (ns != 0)
			_telnet.Controller.CopyBlock(_telnet.Controller.CursorAddress + nn, _telnet.Controller.CursorAddress, ns, true);

		/* Clear the end of the line */
		_telnet.Controller.EraseRegion(_telnet.Controller.CursorAddress + ns, nn, true);
		return AnsiState.DATA;
	}

	private AnsiState ansi_sgr(int ig1, int ig2)
	{
		int i;

		for (i = 0; i <= Nx && i < NN; i++)
			switch (N[i])
			{
				case 0:
					Gr = 0;
					Fg = 0;
					Bg = 0;
					break;
				case 1:
					Gr |= ExtendedAttribute.GR_INTENSIFY;
					break;
				case 4:
					Gr |= ExtendedAttribute.GR_UNDERLINE;
					break;
				case 5:
					Gr |= ExtendedAttribute.GR_BLINK;
					break;
				case 7:
					Gr |= ExtendedAttribute.GR_REVERSE;
					break;
				case 30:
					Fg = 0xf0; /* black */
					break;
				case 31:
					Fg = 0xf2; /* red */
					break;
				case 32:
					Fg = 0xf4; /* green */
					break;
				case 33:
					Fg = 0xf6; /* yellow */
					break;
				case 34:
					Fg = 0xf1; /* blue */
					break;
				case 35:
					Fg = 0xf3; /* megenta */
					break;
				case 36:
					Fg = 0xfd; /* cyan */
					break;
				case 37:
					Fg = 0xff; /* white */
					break;
				case 39:
					Fg = 0; /* default */
					break;
				case 40:
					Bg = 0xf0; /* black */
					break;
				case 41:
					Bg = 0xf2; /* red */
					break;
				case 42:
					Bg = 0xf4; /* green */
					break;
				case 43:
					Bg = 0xf6; /* yellow */
					break;
				case 44:
					Bg = 0xf1; /* blue */
					break;
				case 45:
					Bg = 0xf3; /* megenta */
					break;
				case 46:
					Bg = 0xfd; /* cyan */
					break;
				case 47:
					Bg = 0xff; /* white */
					break;
				case 49:
					Bg = 0; /* default */
					break;
			}

		return AnsiState.DATA;
	}

	private AnsiState ansi_bell(int ig1, int ig2)
	{
		//ring_bell();
		return AnsiState.DATA;
	}

	private AnsiState ansi_newpage(int ig1, int ig2)
	{
		_telnet.Controller.Clear(false);
		return AnsiState.DATA;
	}

	private AnsiState ansi_backspace(int ig1, int ig2)
	{
		if (HeldWrap)
		{
			HeldWrap = false;
			return AnsiState.DATA;
		}

		if (RevWraparoundMode)
		{
			if (_telnet.Controller.CursorAddress > (ScrollTop - 1) * _telnet.Controller.ColumnCount)
				_telnet.Controller.SetCursorAddress(_telnet.Controller.CursorAddress - 1);
		}
		else
		{
			if (_telnet.Controller.CursorAddress % _telnet.Controller.ColumnCount != 0)
				_telnet.Controller.SetCursorAddress(_telnet.Controller.CursorAddress - 1);
		}

		return AnsiState.DATA;
	}

	private AnsiState ansi_cr(int ig1, int ig2)
	{
		if (_telnet.Controller.CursorAddress % _telnet.Controller.ColumnCount != 0)
			_telnet.Controller.SetCursorAddress(_telnet.Controller.CursorAddress - _telnet.Controller.CursorAddress % _telnet.Controller.ColumnCount);
		if (AutoNewlineMode)
			ansi_lf(0, 0);
		HeldWrap = false;
		return AnsiState.DATA;
	}

	private AnsiState ansi_lf(int ig1, int ig2)
	{
		var nc = _telnet.Controller.CursorAddress + _telnet.Controller.ColumnCount;

		HeldWrap = false;

		/* If we're below the scrolling region, don't scroll. */
		if (_telnet.Controller.CursorAddress / _telnet.Controller.ColumnCount >= ScrollBottom)
		{
			if (nc < _telnet.Controller.RowCount * _telnet.Controller.ColumnCount)
				_telnet.Controller.SetCursorAddress(nc);
			return AnsiState.DATA;
		}

		if (nc < ScrollBottom * _telnet.Controller.ColumnCount)
			_telnet.Controller.SetCursorAddress(nc);
		else
			ansi_scroll();
		return AnsiState.DATA;
	}

	private AnsiState ansi_htab(int ig1, int ig2)
	{
		var col = _telnet.Controller.CursorAddress % _telnet.Controller.ColumnCount;
		int i;

		HeldWrap = false;
		if (col == _telnet.Controller.ColumnCount - 1)
			return AnsiState.DATA;
		for (i = col + 1; i < _telnet.Controller.ColumnCount - 1; i++)
			if ((Tabs[i / 8] & (1 << (i % 8))) != 0)
				break;
		_telnet.Controller.SetCursorAddress(_telnet.Controller.CursorAddress - col + i);
		return AnsiState.DATA;
	}

	private AnsiState ansi_escape(int ig1, int ig2)
	{
		return AnsiState.ESC;
	}

	private AnsiState ansi_nop(int ig1, int ig2)
	{
		return AnsiState.DATA;
	}

	private void Pwrap()
	{
		var nc = _telnet.Controller.CursorAddress + 1;
		if (nc < ScrollBottom * _telnet.Controller.ColumnCount)
		{
			_telnet.Controller.SetCursorAddress(nc);
		}
		else
		{
			if (_telnet.Controller.CursorAddress / _telnet.Controller.ColumnCount >= ScrollBottom)
			{
				_telnet.Controller.SetCursorAddress(_telnet.Controller.CursorAddress / _telnet.Controller.ColumnCount * _telnet.Controller.ColumnCount);
			}
			else
			{
				ansi_scroll();
				_telnet.Controller.SetCursorAddress(nc - _telnet.Controller.ColumnCount);
			}
		}
	}


	private AnsiState ansi_printing(int ig1, int ig2)
	{
		if (HeldWrap)
		{
			Pwrap();
			HeldWrap = false;
		}

		if (AnsiInsertMode)
			ansi_insert_chars(1, 0);
		switch (Csd[OnceCset != -1 ? OnceCset : Cset])
		{
			case CSD_LD: /* line drawing "0" */
				if (AnsiCh >= 0x5f && AnsiCh <= 0x7e)
					_telnet.Controller.AddCharacter(_telnet.Controller.CursorAddress, (byte)(AnsiCh - 0x5f),
						2);
				else
					_telnet.Controller.AddCharacter(_telnet.Controller.CursorAddress, Tables.Ascii2Cg[AnsiCh], 0);
				break;
			case CSD_UK: /* UK "A" */
				if (AnsiCh == '#')
					_telnet.Controller.AddCharacter(_telnet.Controller.CursorAddress, 0x1e, 2);
				else
					_telnet.Controller.AddCharacter(_telnet.Controller.CursorAddress, Tables.Ascii2Cg[AnsiCh], 0);
				break;
			case CSD_US: /* US "B" */
				_telnet.Controller.AddCharacter(_telnet.Controller.CursorAddress, Tables.Ascii2Cg[AnsiCh], 0);
				break;
		}

		OnceCset = -1;
		_telnet.Controller.ctlr_add_gr(_telnet.Controller.CursorAddress, Gr);
		_telnet.Controller.SetForegroundColor(_telnet.Controller.CursorAddress, Fg);
		_telnet.Controller.SetBackgroundColor(_telnet.Controller.CursorAddress, Bg);
		if (WraparoundMode)
		{
			/*
			 * There is a fascinating behavior of xterm which we will
			 * attempt to emulate here.  When a character is printed in the
			 * last column, the cursor sticks there, rather than wrapping
			 * to the next line.  Another printing character will put the
			 * cursor in column 2 of the next line.  One cursor-left
			 * sequence won't budge it; two will.  Saving and restoring
			 * the cursor won't move the cursor, but will cancel all of
			 * the above behaviors...
			 *
			 * In my opinion, very strange, but among other things, 'vi'
			 * depends on it!
			 */
			if (0 == (_telnet.Controller.CursorAddress + 1) % _telnet.Controller.ColumnCount)
				HeldWrap = true;
			else
				Pwrap();
		}
		else
		{
			if (_telnet.Controller.CursorAddress % _telnet.Controller.ColumnCount != _telnet.Controller.ColumnCount - 1)
				_telnet.Controller.SetCursorAddress(_telnet.Controller.CursorAddress + 1);
		}

		return AnsiState.DATA;
	}

	private AnsiState ansi_semicolon(int ig1, int ig2)
	{
		if (Nx >= NN)
			return AnsiState.DATA;
		Nx++;
		return State;
	}

	private AnsiState ansi_digit(int ig1, int ig2)
	{
		N[Nx] = N[Nx] * 10 + (AnsiCh - '0');
		return State;
	}

	private AnsiState ansi_reverse_index(int ig1, int ig2)
	{
		var rr = _telnet.Controller.CursorAddress / _telnet.Controller.ColumnCount; /* current row */
		var np = ScrollTop - 1 - rr; /* number of rows in the scrolling
				   region, above this line */
		int ns; /* number of rows to scroll */
		var nn = 1; /* number of rows to index */

		HeldWrap = false;

		/* If the cursor is above the scrolling region, do a simple margined
		   cursor up.  */
		if (np < 0)
		{
			ansi_cursor_up(nn, 0);
			return AnsiState.DATA;
		}

		/* Split the number of lines to scroll into ns */
		if (nn > np)
		{
			ns = nn - np;
			nn = np;
		}
		else
		{
			ns = 0;
		}

		/* Move the cursor up without scrolling */
		if (nn != 0)
			ansi_cursor_up(nn, 0);

		/* Insert lines at the top for backward scroll */
		if (ns != 0)
			ansi_insert_lines(ns, 0);

		return AnsiState.DATA;
	}

	private AnsiState ansi_send_attributes(int nn, int ig2)
	{
		if (nn == 0)
			_telnet.SendString("\033[?1;2c");
		return AnsiState.DATA;
	}

	private AnsiState dec_return_terminal_id(int ig1, int ig2)
	{
		return ansi_send_attributes(0, 0);
	}

	private AnsiState ansi_set_mode(int nn, int ig2)
	{
		switch (nn)
		{
			case 4:
				AnsiInsertMode = true;
				break;
			case 20:
				AutoNewlineMode = true;
				break;
		}

		return AnsiState.DATA;
	}

	private AnsiState ansi_reset_mode(int nn, int ig2)
	{
		switch (nn)
		{
			case 4:
				AnsiInsertMode = false;
				break;
			case 20:
				AutoNewlineMode = false;
				break;
		}

		return AnsiState.DATA;
	}

	private AnsiState ansi_status_report(int nn, int ig2)
	{
		string ansiStatusCpr;

		switch (nn)
		{
			case 5:
				_telnet.SendString("\033[0n");
				break;
			case 6:
				ansiStatusCpr = "\033[" + (_telnet.Controller.CursorAddress / _telnet.Controller.ColumnCount + 1) + ";" +
				                (_telnet.Controller.CursorAddress % _telnet.Controller.ColumnCount + 1) + "R";
				_telnet.SendString(ansiStatusCpr);
				break;
		}

		return AnsiState.DATA;
	}

	private AnsiState ansi_cs_designate(int ig1, int ig2)
	{
		CsToChange = Gnnames.IndexOf(AnsiCh); //strchr(gnnames, ansi_ch) - gnnames;
		return AnsiState.CSDES;
	}

	private AnsiState ansi_cs_designate2(int ig1, int ig2)
	{
		Csd[CsToChange] = Csnames.IndexOf(AnsiCh); //strchr(csnames, ansi_ch) - csnames;
		return AnsiState.DATA;
	}

	private AnsiState ansi_select_g0(int ig1, int ig2)
	{
		Cset = CS_G0;
		return AnsiState.DATA;
	}

	private AnsiState ansi_select_g1(int ig1, int ig2)
	{
		Cset = CS_G1;
		return AnsiState.DATA;
	}

	private AnsiState ansi_select_g2(int ig1, int ig2)
	{
		Cset = CS_G2;
		return AnsiState.DATA;
	}

	private AnsiState ansi_select_g3(int ig1, int ig2)
	{
		Cset = CS_G3;
		return AnsiState.DATA;
	}

	private AnsiState ansi_one_g2(int ig1, int ig2)
	{
		OnceCset = CS_G2;
		return AnsiState.DATA;
	}

	private AnsiState ansi_one_g3(int ig1, int ig2)
	{
		OnceCset = CS_G3;
		return AnsiState.DATA;
	}

	private AnsiState ansi_esc3(int ig1, int ig2)
	{
		return AnsiState.DECP;
	}

	private AnsiState dec_set(int ig1, int ig2)
	{
		int i;

		for (i = 0; i <= Nx && i < NN; i++)
			switch (N[i])
			{
				case 1: /* application cursor keys */
					ApplCursor = 1;
					break;
				case 2: /* set G0-G3 */
					Csd[0] = Csd[1] = Csd[2] = Csd[3] = CSD_US;
					break;
				case 3: /* 132-column mode */
					if (AllowWideMode) WideMode = true;
					//screen_132();
					break;
				case 7: /* wraparound mode */
					WraparoundMode = true;
					break;
				case 40: /* allow 80/132 switching */
					AllowWideMode = true;
					break;
				case 45: /* reverse-wraparound mode */
					RevWraparoundMode = true;
					break;
				case 47: /* alt buffer */
					_telnet.Controller.SwapAltBuffers(true);
					break;
			}

		return AnsiState.DATA;
	}

	private AnsiState dec_reset(int ig1, int ig2)
	{
		int i;

		for (i = 0; i <= Nx && i < NN; i++)
			switch (N[i])
			{
				case 1: /* normal cursor keys */
					ApplCursor = 0;
					break;
				case 3: /* 132-column mode */
					if (AllowWideMode) WideMode = false;
					//				screen_80();
					break;
				case 7: /* no wraparound mode */
					WraparoundMode = false;
					break;
				case 40: /* allow 80/132 switching */
					AllowWideMode = false;
					break;
				case 45: /* no reverse-wraparound mode */
					RevWraparoundMode = false;
					break;
				case 47: /* alt buffer */
					_telnet.Controller.SwapAltBuffers(false);
					break;
			}

		return AnsiState.DATA;
	}

	private AnsiState dec_save(int ig1, int ig2)
	{
		int i;

		for (i = 0; i <= Nx && i < NN; i++)
			switch (N[i])
			{
				case 1: /* application cursor keys */
					SavedApplCursor = ApplCursor;
					break;
				case 3: /* 132-column mode */
					SavedWideMode = WideMode;
					break;
				case 7: /* wraparound mode */
					SavedWraparoundMode = WraparoundMode;
					break;
				case 40: /* allow 80/132 switching */
					SavedAllowWideMode = AllowWideMode;
					break;
				case 45: /* reverse-wraparound mode */
					SavedRevWraparoundMode = RevWraparoundMode;
					break;
				case 47: /* alt buffer */
					SavedAltbuffer = _telnet.Controller.IsAltBuffer;
					break;
			}

		return AnsiState.DATA;
	}

	private AnsiState dec_restore(int ig1, int ig2)
	{
		int i;

		for (i = 0; i <= Nx && i < NN; i++)
			switch (N[i])
			{
				case 1: /* application cursor keys */
					ApplCursor = SavedApplCursor;
					break;
				case 3: /* 132-column mode */
					if (AllowWideMode) WideMode = SavedWideMode;
					break;
				case 7: /* wraparound mode */
					WraparoundMode = SavedWraparoundMode;
					break;
				case 40: /* allow 80/132 switching */
					AllowWideMode = SavedAllowWideMode;
					break;
				case 45: /* reverse-wraparound mode */
					RevWraparoundMode = SavedRevWraparoundMode;
					break;
				case 47: /* alt buffer */
					_telnet.Controller.SwapAltBuffers(SavedAltbuffer);
					break;
			}

		return AnsiState.DATA;
	}

	private AnsiState dec_scrolling_region(int top, int bottom)
	{
		if (top < 1)
			top = 1;
		if (bottom > _telnet.Controller.RowCount)
			bottom = _telnet.Controller.RowCount;
		if (top <= bottom && (top > 1 || bottom < _telnet.Controller.RowCount))
		{
			ScrollTop = top;
			ScrollBottom = bottom;
			_telnet.Controller.SetCursorAddress(0);
		}
		else
		{
			ScrollTop = 1;
			ScrollBottom = _telnet.Controller.RowCount;
		}

		return AnsiState.DATA;
	}

	private AnsiState xterm_text_mode(int ig1, int ig2)
	{
		Nx = 0;
		N[0] = 0;
		return AnsiState.TEXT;
	}

	private AnsiState xterm_text_semicolon(int ig1, int ig2)
	{
		Tx = 0;
		return AnsiState.TEXT2;
	}

	private AnsiState xterm_text(int ig1, int ig2)
	{
		if (Tx < NT)
		{
			Text += AnsiCh;
			Tx++;
		}

		return State;
	}

	private AnsiState xterm_text_do(int ig1, int ig2)
	{
		return AnsiState.DATA;
	}

	private AnsiState ansi_htab_set(int ig1, int ig2)
	{
		var col = _telnet.Controller.CursorAddress % _telnet.Controller.ColumnCount;

		Tabs[col / 8] = (byte)(Tabs[col / 8] | (1 << (col % 8)));
		return AnsiState.DATA;
	}

	private AnsiState ansi_htab_clear(int nn, int ig2)
	{
		int col, i;

		switch (nn)
		{
			case 0:
				col = _telnet.Controller.CursorAddress % _telnet.Controller.ColumnCount;
				Tabs[col / 8] = (byte)(Tabs[col / 8] & ~(1 << (col % 8)));
				break;
			case 3:
				for (i = 0; i < (_telnet.Controller.ColumnCount + 7) / 8; i++)
					Tabs[i] = 0;
				break;
		}

		return AnsiState.DATA;
	}

	/*
	 * Scroll the screen or the scrolling region.
	 */
	private void ansi_scroll()
	{
		HeldWrap = false;

		/* Save the top line */
		if (ScrollTop == 1 && ScrollBottom == _telnet.Controller.RowCount)
		{
			_telnet.Controller.ScrollOne();
			return;
		}

		/* Scroll all but the last line up */
		if (ScrollBottom > ScrollTop)
			_telnet.Controller.CopyBlock(ScrollTop * _telnet.Controller.ColumnCount,
				(ScrollTop - 1) * _telnet.Controller.ColumnCount,
				(ScrollBottom - ScrollTop) * _telnet.Controller.ColumnCount,
				true);

		/* Clear the last line */
		_telnet.Controller.EraseRegion((ScrollBottom - 1) * _telnet.Controller.ColumnCount, _telnet.Controller.ColumnCount, true);
	}

	/* Callback for when we enter ANSI mode. */
	public void ansi_in3270(bool in3270)
	{
		if (!in3270)
			ansi_reset(0, 0);
	}

	/*
	 * External entry points
	 */
	public void ansi_init()
	{
		_telnet.Connected3270 += telnet_Connected3270;
	}

	private void telnet_Connected3270(object sender, Connected3270EventArgs e)
	{
		ansi_in3270(e.Is3270);
	}


	public void ansi_process(byte c)
	{
		c &= 0xff;
		AnsiCh = (char)c;


		if (_telnet.Appres.Toggled(Appres.ScreenTrace)) _telnet.Trace.trace_char((char)c);

		var s = _st[(int)State];
		var bs = (byte[])s;
		int fnindex = bs[c];
		var fn = _ansiFn[fnindex];
		State = fn(N[0], N[1]);
	}

	public void ansi_send_up()
	{
		if (ApplCursor != 0)
			_telnet.SendString("\033OA");
		else
			_telnet.SendString("\033[A");
	}

	public void ansi_send_down()
	{
		if (ApplCursor != 0)
			_telnet.SendString("\033OB");
		else
			_telnet.SendString("\033[B");
	}

	public void ansi_send_right()
	{
		if (ApplCursor != 0)
			_telnet.SendString("\033OC");
		else
			_telnet.SendString("\033[C");
	}

	public void ansi_send_left()
	{
		if (ApplCursor != 0)
			_telnet.SendString("\033OD");
		else
			_telnet.SendString("\033[D");
	}

	public void ansi_send_home()
	{
		_telnet.SendString("\033[H");
	}

	public void ansi_send_clear()
	{
		_telnet.SendString("\033[2K");
	}

	public void ansi_send_pf(int nn)
	{
		throw new ApplicationException("ansi_send_pf not implemented");
	}

	public void ansi_send_pa(int nn)
	{
		throw new ApplicationException("ansi_send_pa not implemented");
	}
}
