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
using Open3270.TN3270;

namespace Open3270.TN3270Server;

/// <summary>
///     Summary description for TNServerScreen.
/// </summary>
public class TnServerScreen
{
	//
	private const int ATTR_PROTECT_BIT = 0x02;
	private const int ATTR_3270_PROTECT_BIT = 0x20;
	private const int ATTR_3270_NUMONLY = 0x10;

	private const int ATTR_BOLD_BIT = 0x08;
	private const int ATTR_SELECT_BIT = 0x04;
	private const int ATTR_MORE_BIT = 0x10;
	private const int ATTR_MDT_BIT = 0x01;

	private const int ATTR_NORM = ATTR_PROTECT_BIT;
	private const int ATTR_BOLD = ATTR_PROTECT_BIT | ATTR_BOLD_BIT;
	private const int ATTR_INP = ATTR_MORE_BIT;
	private const int ATTR_INP_BOLD = ATTR_BOLD_BIT;
	private const int ATTR_HIDDEN = ATTR_PROTECT_BIT | ATTR_BOLD_BIT | ATTR_SELECT_BIT | ATTR_MDT_BIT;
	private const int ATTR_PASSWORD = ATTR_SELECT_BIT | ATTR_BOLD_BIT;

	private const int ATTR_3270_NORM = 0xC0 | ATTR_3270_PROTECT_BIT | ATTR_3270_NUMONLY; // make it autoskip
	private const int ATTR_3270_BOLD = 0xC0 | ATTR_3270_PROTECT_BIT | ATTR_BOLD_BIT | ATTR_3270_NUMONLY;
	private const int ATTR_3270_INPUT = 0xC0;
	private const int ATTR_3270_INPUT_BOLD = 0xC0 | ATTR_BOLD_BIT;
	private const int ATTR_3270_HIDDEN = ATTR_3270_PROTECT_BIT | ATTR_BOLD_BIT | ATTR_SELECT_BIT | ATTR_MDT_BIT;
	private const int ATTR_3270_PASSWORD = 0xC0 | ATTR_PASSWORD;

	private const byte WCC_CLEARMDT = 0x01;
	private const byte WCC_UNLOCK = 0x02;
	private const byte WCC_BASE = 0xC0;

	private static readonly byte[] InboundAddrChars = new byte[]
	{
		0x40, 0xC1, 0xC2, 0xC3, 0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F,
		0x50, 0xD1, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0x5A, 0x5B, 0x5C, 0x5D, 0x5E, 0x5F,
		0x60, 0x61, 0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0x6A, 0x6B, 0x6C, 0x6D, 0x6E, 0x6F,
		0xF0, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8, 0xF9, 0x7A, 0x7B, 0x7C, 0x7D, 0x7E, 0x7F
	};

	public int CurrentCursorPosition;
	public bool FFormatted = true;
	private Hashtable _hashAidToText;

	//
	private Hashtable _hashTextToAid;
	public bool InExtendedMode = false;


	public byte LastAid;
	public byte[] MScreenBytes;
	private readonly ArrayList _mStrings;
	private bool _mStringsLive = true;


	public TnServerScreen(int cx, int cy)
	{
		MScreenBytes = new byte[cx * cy];
		_mStrings = new ArrayList();
	}

	public void Clear()
	{
		_mStrings.Clear();
		_mStringsLive = true;
	}

	public void Add(string text)
	{
		if (!_mStringsLive)
			Clear();
		_mStrings.Add(text);
	}

	public void SetCursor(int x, int y)
	{
		CurrentCursorPosition = x + y * 80;
	}

	public void Format()
	{
		MScreenBytes = FormatScreen((string[])_mStrings.ToArray(typeof(string)));
	}

	private byte[] FormatScreen(string[] data)
	{
		//
		// Step 1 - map screen image into buffer
		//
		var buffer = new byte[10000];
		var to = 0;
		int i;
		int start, end;
		//

		//
		for (i = 0; i < 24; i++)
		{
			var count = 0;
			if (i < data.Length && data[i] != null)
				count += CopyMapData(data[i], 0, buffer, to, true);
			while (count < 80)
			{
				buffer[to + count] = 0x40;
				count++;
			}

			to += 80; //count;
		}

		//
		// Step 2 - clear trailing spaces on each field
		//
		i = 0;
		while (i < to)
			if (Isattrib(buffer[i]) &&
			    Isentry(buffer[i]))
			{
				i++;
				start = i;
				while (i < to)
				{
					if (Isattrib(buffer[i]))
						break;
					i++;
				}

				if (i == to)
					break;
				end = i - 1;
				while (end >= start)
				{
					if (buffer[end] != 0x40)
						break;
					buffer[end] = 0;
					end--;
				}
			}
			else
			{
				i++;
			}

		//
		// Step 3 - return the screen
		//
		var response = new byte[to];
		for (i = 0; i < to; i++) response[i] = buffer[i];
		return response;
	}

	private bool Isattrib(byte c)
	{
		if (c != 0 && c < 0x20)
			return true;
		return false;
		//return ((c && (c < 0x20))!=0);
	}

	private bool Isentry(byte c)
	{
		return 0 != (c & ATTR_PROTECT_BIT);
	}


	//
	private int CopyMapData(string from, int fromIndex, byte[] to, int toIndex, bool fScreen)
		// PCH pchFrom, PCH pchTo, BOOL fScreen, int rows, int columns)
	{
		var toSave = toIndex;
		while (fromIndex < from.Length && from[fromIndex] != 0)
		{
			if (fScreen)
				switch (from[fromIndex])
				{
					case ']': to[toIndex++] = ATTR_NORM; break;
					case '}': to[toIndex++] = ATTR_BOLD; break;
					case '[': to[toIndex++] = ATTR_INP; break;
					case '{': to[toIndex++] = ATTR_INP_BOLD; break;
					case '~': to[toIndex++] = ATTR_HIDDEN; break;
					case '^': to[toIndex++] = ATTR_PASSWORD; break;
					default:
						to[toIndex++] = Tables.A2E[(byte)from[fromIndex]];
						break;
				}
			else
				switch (from[fromIndex])
				{
					case ']': to[toIndex++] = ATTR_3270_NORM; break;
					case '}': to[toIndex++] = ATTR_3270_BOLD; break;
					case '[': to[toIndex++] = ATTR_3270_INPUT; break;
					case '{': to[toIndex++] = ATTR_3270_INPUT_BOLD; break;
					case '~': to[toIndex++] = ATTR_3270_HIDDEN; break;
					case '^': to[toIndex++] = ATTR_3270_PASSWORD; break;
					default:
						to[toIndex++] = Tables.A2E[(byte)from[fromIndex]];
						break;
				}

			fromIndex++;
		}

		return toIndex - toSave;
	}

	public byte[] AsTn3270Buffer(bool fClear, bool fUnlock, bool tn3270E)
	{
		if (_mStringsLive)
		{
			Format();
			_mStringsLive = false;
		}

		var buffer = new byte[10000];
		var to = 0;
		var from = 0;
		var fProtected = true;
		var wcc = WCC_BASE;
		var blankCount = 0;
		var currentOffset = 0;
		//
		//
		if (tn3270E)
		{
			buffer[to++] = 0x07;
			buffer[to++] = 0x00;
			buffer[to++] = 0x00;
			buffer[to++] = 0x00;
			buffer[to++] = 0x00;
		}

		//
		if (FFormatted || fClear)
		{
			if (fClear)
				buffer[to++] = 0x05;
			else
				buffer[to++] = 0x01;

			if (fUnlock)
				wcc |= WCC_UNLOCK;
			//
			buffer[to++] = wcc;
		}

		//
		while (from < MScreenBytes.Length)
		{
			if (Isattrib(MScreenBytes[from]))
			{
				if (blankCount > 0)
				{
					to += FlushBlanks(buffer, to, blankCount, currentOffset);
					blankCount = 0;
				}

				buffer[to++] = See.ORDER_SF;
				switch (MScreenBytes[from])
				{
					case ATTR_NORM:
						buffer[to++] = ATTR_3270_NORM;
						fProtected = true;
						break;
					case ATTR_BOLD:
						buffer[to++] = ATTR_3270_BOLD;
						fProtected = true;
						break;
					case ATTR_HIDDEN:
						buffer[to++] = ATTR_3270_HIDDEN;
						fProtected = true;
						break;
					case ATTR_INP:
						buffer[to++] = ATTR_3270_INPUT;
						fProtected = false;
						break;
					case ATTR_INP_BOLD:
						buffer[to++] = ATTR_3270_INPUT_BOLD;
						fProtected = false;
						break;
					case ATTR_PASSWORD:
						buffer[to++] = ATTR_3270_PASSWORD;
						fProtected = false;
						break;
				}
			}
			else if (fProtected && (MScreenBytes[from] == 0 || MScreenBytes[from] == 0x40))
			{
				blankCount++;
			}
			else
			{
				if (blankCount > 0)
				{
					to += FlushBlanks(buffer, to, blankCount, currentOffset);
					blankCount = 0;
				}

				buffer[to++] = MScreenBytes[from];
			}

			from++;
			currentOffset++;
		}

		//
		// Now send formatted formatted
		//
		if (FFormatted)
		{
			buffer[to++] = See.ORDER_SBA;
			to += Create12BitAddress(buffer, to, CurrentCursorPosition);
			buffer[to++] = See.ORDER_IC;
		}

		//
		// End of buffer
		//
		buffer[to++] = 0xFF;
		buffer[to++] = 0xEF;
		//
		// ok - length of buffer is "to", send the buffer
		//
		var ret = new byte[to];
		for (var i = 0; i < to; i++) ret[i] = buffer[i];
		return ret;
	}

	private int FlushBlanks(byte[] data, int to, int count, int currentOffset)
	{
		var offset = 0;
		if (count < 5)
		{
			while (count-- > 0)
			{
				data[to + offset] = 0x40;
				offset++;
			}
		}
		else
		{
			data[to] = See.ORDER_RA;
			offset++;
			offset += Create12BitAddress(data, to + offset, currentOffset);
			data[to] = 0x00;
			offset++;
		}

		return offset;
	}

	private int Create12BitAddress(byte[] data, int to, int address)
	{
		data[to++] = InboundAddrChars[address >> 6];
		data[to++] = InboundAddrChars[address & 0x003F]; // xxxx xxxx xx11 1111
		return 2;
	}

	private int Bufaddr(byte[] data, int offset)
	{
		return ((data[offset] & 0x3f) << 6) + (data[offset + 1] & 0x3f);
	}

	public string HandleTn3270Data(byte[] data, int length)
	{
		var state = Tns.DO_AID;
		var stateData = new byte[8];
		var fInField = false;

		var offset = 0;
		var currentScreenOffset = 0;
		if (InExtendedMode) offset += 5;
		if (!FFormatted)
			switch (data[offset])
			{
				case 0x6d:
				case 0x7d:
					state = Tns.DO_AID;
					break;
				default:
					LastAid = 0x7d;
					state = Tns.DO_DATA;
					break;
			}

		while (offset < length)
		{
			switch (state)
			{
				case Tns.DO_AID:
					LastAid = data[offset];
					state = Tns.DO_CURADDR1;
					break;
				case Tns.DO_CURADDR1:
					stateData[0] = data[offset];
					state = Tns.DO_CURADDR2;
					break;
				case Tns.DO_CURADDR2:
					stateData[1] = data[offset];
					CurrentCursorPosition = Bufaddr(stateData, 0);
					if (FFormatted)
						state = Tns.DO_FIRST;
					else
						state = Tns.DO_DATA;
					break;
				case Tns.DO_FIRST:
					if (data[offset] == 0x11)
					{
						state = Tns.DO_SBA1;
					}
					else if (data[offset] == 0xFF)
					{
						state = Tns.DO_IAC;
					}
					else
					{
						Console.WriteLine("TNS.DO_FIRST = {0:X2}", data[offset]);
						throw new ApplicationException("Bad formatted screen response!");
						//return null;
					}

					break;

				case Tns.DO_DATA:
					if (data[offset] == 0x11)
					{
						while (!Isattrib(MScreenBytes[currentScreenOffset]) &&
						       currentScreenOffset < MScreenBytes.Length)
							MScreenBytes[currentScreenOffset++] = 0;

						state = Tns.DO_SBA1;
					}
					else if (data[offset] == 0xFF)
					{
						state = Tns.DO_IAC;
					}
					else
					{
						MScreenBytes[currentScreenOffset++] = data[offset];
					}

					break;
				case Tns.DO_SBA1:
					stateData[0] = data[offset];
					state = Tns.DO_SBA2;
					break;
				case Tns.DO_SBA2:
					stateData[1] = data[offset];
					currentScreenOffset = Bufaddr(stateData, 0);
					state = Tns.DO_DATA;
					fInField = true;
					break;
				case Tns.DO_IAC:
					if (data[offset] == 0xEF)
					{
						if (fInField)
							while (currentScreenOffset < MScreenBytes.Length &&
							       !Isattrib(MScreenBytes[currentScreenOffset]))
								MScreenBytes[currentScreenOffset++] = 0;

						return AidToText(LastAid);
					}

					state = Tns.DO_DATA;
					MScreenBytes[currentScreenOffset++] = data[offset];
					break;
			}

			offset++;
		}

		return AidToText(LastAid);
	}

	//
	/* Key mnemonic translations */
	private byte GetAid(string aidData)
	{
		byte aidKey = 0;
		var offset = 1;

		switch (aidData[offset])
		{
			case 'A':
				if (aidData[offset + 1] != '@')
					return aidKey;
				switch (aidData[offset + 2])
				{
					case 'H':
						aidKey = 0x30;
						break;
					case 'Q':
						aidKey = 0x2D; // ATTN
						break;
					case 'J':
						aidKey = 0x3D;
						break;
					case 'C':
						aidKey = 0x2A;
						break;
					case '<':
						aidKey = 0x3D; // record backspace
						break;
					default:
						return aidKey;
				}
				break;

			case 'E': // Enter
				aidKey = 0x27;
				break;
			case 'C': // Clear
				aidKey = 0x5F;
				break;
			case 'H': // Help
				aidKey = 0x2b;
				break;
			case 'P': // Print
				aidKey = 0x2f;
				break;
			case '1': // PF1
				aidKey = 0x31;
				break;
			case '2': // PF2
				aidKey = 0x32;
				break;
			case '3': // PF3
				aidKey = 0x33;
				break;
			case '4': // PF4
				aidKey = 0x34;
				break;
			case '5': // PF5
				aidKey = 0x35;
				break;
			case '6': // PF6
				aidKey = 0x36;
				break;
			case '7': // PF7
				aidKey = 0x37;
				break;
			case '8': // PF8
				aidKey = 0x38;
				break;
			case '9': // PF9
				aidKey = 0x39;
				break;
			case 'a': // PF10
				aidKey = 0x3A;
				break;
			case 'b': // PF11
				aidKey = 0x23;
				break;
			case 'c': // PF12
				aidKey = 0x40;
				break;
			case 'd': // PF13
				aidKey = 0x41;
				break;
			case 'e': // PF14
				aidKey = 0x42;
				break;
			case 'f': // PF15
				aidKey = 0x43;
				break;
			case 'g': // PF16
				aidKey = 0x44;
				break;
			case 'h': // PF17
				aidKey = 0x45;
				break;
			case 'i': // PF18
				aidKey = 0x46;
				break;
			case 'j': // PF19
				aidKey = 0x47;
				break;
			case 'k': // PF20
				aidKey = 0x48;
				break;
			case 'l': // PF21
				aidKey = 0x49;
				break;
			case 'm': // PF22
				aidKey = 0x5B;
				break;
			case 'n': // PF23
				aidKey = 0x2E;
				break;
			case 'o': // PF24
				aidKey = 0x3C;
				break;
			case 'u': // PgUp
				aidKey = 0x25;
				break;
			case 'v': // PgDown
				aidKey = 0x3e;
				break;
			case 'x': // PA1
				aidKey = 0x25;
				break;
			case 'y': // PA2
				aidKey = 0x3E;
				break;
			case 'z': // PA3
				aidKey = 0x2C;
				break;
		} // end of switch

		return Tables.A2E[aidKey];
	}

	private void InitAidTable()
	{
		_hashTextToAid = new Hashtable
		{
			["tab"] = "@T",
			["enter"] = "@E",
			["clear"] = "@C",
			["home"] = "@0",
			["erase eof"] = "@F",
			["eraseeof"] = "@F",
			["pf1"] = "@1",
			["pf2"] = "@2",
			["pf3"] = "@3",
			["pf4"] = "@4",
			["pf5"] = "@5",
			["pf6"] = "@6",
			["pf7"] = "@7",
			["pf8"] = "@8",
			["pf9"] = "@9",
			["pf10"] = "@a",
			["pf11"] = "@b",
			["pf12"] = "@c",
			["pf13"] = "@d",
			["pf14"] = "@e",
			["pf15"] = "@f",
			["pf16"] = "@g",
			["pf17"] = "@h",
			["pf18"] = "@i",
			["pf19"] = "@j",
			["pf20"] = "@k",
			["pf21"] = "@l",
			["pf22"] = "@m",
			["pf23"] = "@n",
			["pf24"] = "@o",
			["left tab"] = "@B",
			["back tab"] = "@B",
			["lefttab"] = "@B",
			["backtab"] = "@B",
			["put"] = "@S@p", //       // [put](F_row_col, "text";
			["c_pos"] = "@S@c", //     // [c_pos](offset;
			["buffers"] = "@S@d", //   // [buffers](hostwritesexpected;
			["sleep"] = "@S@e", //     // [sleep](milliseconds;
			["settle"] = "@S@f", //    // [settle](settletime;
			["delete"] = "@D",
			["help"] = "@H",
			["insert"] = "@I",
			["left"] = "@L",
			["new line"] = "@N",
			["newline"] = "@N",
			["space"] = "@O",
			["print"] = "@P",
			["reset"] = "@R",
			["up"] = "@U",
			["down"] = "@V",
			["right"] = "@Z",
			["plus"] = "@p",
			["end"] = "@q",
			["page up"] = "@u",
			["page down"] = "@v",
			["pageup"] = "@u",
			["pagedown"] = "@v",
			["recordback"] = "@A@<",
			["recbksp"] = "@A@<",
			["pa1"] = "@x",
			["pa2"] = "@y",
			["pa3"] = "@z",
			["word delete"] = "@A@D",
			["field exit"] = "@A@E",
			["erase input"] = "@A@F",
			["worddelete"] = "@A@D",
			["fieldexit"] = "@A@E",
			["eraseinput"] = "@A@F",
			["sysreq"] = "@A@H",
			["insert"] = "@A@I",
			["cur select"] = "@A@J",
			["curselect"] = "@A@J",
			["attn"] = "@A@Q",
			["printps"] = "@A@T",
			["erase eol"] = "@S@A",
			["eraseeol"] = "@S@A",
			["test"] = "@A@C"
		};

		_hashAidToText = new Hashtable();
		foreach (DictionaryEntry de in _hashTextToAid)
		{
			var v = (string)de.Value;
			var vCode = GetAid(v);
			_hashAidToText[vCode] = (string)de.Key;
		}
	}

	public string AidToText(byte aid)
	{
		if (_hashTextToAid == null) InitAidTable();
		return (string)_hashAidToText[aid];
	}

	public byte TextToAid(string pszText)
	{
		if (_hashTextToAid == null) InitAidTable();

		return _hashTextToAid?[pszText.ToLower()] is not string aidKey ? (byte)0 : GetAid(aidKey);
	}

	public void ClearField(int x, int y, bool multiline)
	{
		int position;
		int end;
		if (multiline)
			FindField(ToCursorPosition(x, y), 80 * 24, out position, out end);
		else
			FindField(ToCursorPosition(x, y), ToCursorPosition(79, y), out position, out end);

		WriteScreen(position, end, true, null);
	}

	public void WriteField(int x, int y, bool direct, string text)
	{
		WriteField(ToCursorPosition(x, y), direct, text);
	}

	public void WriteField(int position, bool direct, string text)
	{
		FindField(position, 80 * 24, out position, out var end);
		WriteScreen(position, end, direct, text);
	}

	public string ReadField(int x, int y)
	{
		return ReadField(ToCursorPosition(x, y));
	}

	public string ReadField(int position)
	{
		//Console.WriteLine("position is "+position);
		FindField(position, 80 * 24, out position, out var end);
		//
		//Console.WriteLine("start is "+position+", end is "+end);
		return ReadScreen(position, end);
	}

	public string ReadScreen(int position, int end)
	{
		int i;
		var text = "";
		for (i = position; i < end; i++) text += Convert.ToChar(Tables.E2A[MScreenBytes[i]]);
		return text.TrimEnd();
	}

	public void WriteFormattedData(int x, int y, string text)
	{
		var position = ToCursorPosition(x, y);
		CopyMapData(text, 0, MScreenBytes, position, true);
	}

	public void WriteScreen(int position, int end, bool direct, string text)
	{
		//Console.WriteLine("position = "+position+", end ="+end);
		if (direct)
		{
			int i;
			for (i = position; i < end; i++)
			{
				char ch;
				if (text != null && i - position < text.Length)
					ch = text[i - position];
				else
					ch = ' ';
				var b = Tables.A2E[ch];
				MScreenBytes[i] = b;
			}
		}
		else
		{
			if (text == null)
				text = "";
			while (text.Length < end - position)
				text = text + " ";

			CopyMapData(text, 0, MScreenBytes, position, true);
		}
	}

	public void FindField(int startposition, int max, out int position, out int end)
	{
		position = startposition;
		while (position < max && Isattrib(MScreenBytes[position]))
			position++;
		end = position;
		while (end < max && !Isattrib(MScreenBytes[end]))
			end++;
	}

	public int ToCursorPosition(int x, int y)
	{
		return x + y * 80;
	}

	private enum Tns
	{
		DO_AID,
		DO_CURADDR1,
		DO_CURADDR2,
		DO_FIRST,
		DO_DATA,
		DO_SBA1,
		DO_SBA2,
		DO_IAC
	}
}
