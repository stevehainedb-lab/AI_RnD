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
// ReSharper disable UnusedMember.Local

namespace Open3270.TN3270;

internal enum Cs
{
	Waiting,
	R_IAC,
	R_SB,
	R_DATA,
	R_IAC_END,
	R_WILL,
	R_WONT,
	R_HEADER,
	R_HEADERDATA
}

/// <summary>
///     Summary description for TN3270ClientParser.
/// </summary>
public class Tn3270ClientParser
{
	//
	private const byte IAC = 255; //Convert.ToChar(255);  // 0xff
	private const byte DO = 253; //Convert.ToChar(253); // 
	private const byte DONT = 254; //Convert.ToChar(254);
	private const byte WILL = 251; //Convert.ToChar(251);

	private const byte WONT = 252; //Convert.ToChar(252);

	//const byte SB					= 250;//Convert.ToChar(250);
	//const byte SE					= 240;//Convert.ToChar(240);
	//const byte EOR					= 239;//Convert.ToChar(240);
	private const byte SB = 250; /* interpret as subnegotiation */
	private const byte GA = 249; /* you may reverse the line */
	private const byte EL = 248; /* erase the current line */
	private const byte EC = 247; /* erase the current character */
	private const byte AYT = 246; /* are you there */
	private const byte AO = 245; /* abort output--but let prog finish */
	private const byte IP = 244; /* interrupt process--permanently */
	private const byte BREAK = 243; /* break */
	private const byte DM = 242; /* data mark--for connect. cleaning */
	private const byte NOP = 241; /* nop */
	private const byte SE = 240; /* end sub negotiation */
	private const byte EOR = 239; /* end of record (transparent mode) */ //0xef
	private const byte SUSP = 237; /* suspend process */
	private const byte XEOF = 236; /* end of file */


	private const byte SYNCH = 242; /* for telfunc calls */

	private const char IS = '0';
	private const char SEND = '1';
	private const char INFO = '2';
	private const char VAR = '0';
	private const char VALUE = '1';
	private const char ESC = '2';
	private const char USERVAR = '3';
	private const int COLS = 80;

	//
	private Cs _cs;
	private readonly byte[] _data;
	private int _datapos;

	/// <summary>
	///     Constructor for the client data parser class
	/// </summary>
	public Tn3270ClientParser()
	{
		_cs = Cs.Waiting;
		_data = new byte[10240];
		_datapos = 0;
	}

	public int BA_TO_ROW(int ba)
	{
		return ba / COLS;
	}

	public int BA_TO_COL(int ba)
	{
		return ba % COLS;
	}

	/// <summary>
	///     Parse the next byte in the client data stream
	/// </summary>
	/// <param name="v"></param>
	public void Parse(byte v)
	{
		Console.WriteLine("" + v);
		switch (_cs)
		{
			case Cs.Waiting:
				if (v == IAC)
				{
					N("IAC");
					_cs = Cs.R_IAC;
				}
				else
				{
					// assume we're reading a header block
					_datapos = 1;
					_data[0] = v;
					_cs = Cs.R_HEADER;
				}

				break;
			case Cs.R_HEADER:
				_data[_datapos] = v;
				_datapos++;
				if (_datapos == TnHeader.EhSize)
				{
					//SH dont know why it creates this
					//new TnHeader(_data);
					_datapos = 0;
					_cs = Cs.R_HEADERDATA;
				}

				break;
			case Cs.R_HEADERDATA:
				_data[_datapos] = v;

				if (_datapos == 0)
					Console.WriteLine(See.GetAidFromCode(v));
				if (_datapos == 2)
					Console.WriteLine(Util.DecodeBAddress(_data[1], _data[2]));

				if (_datapos == 3 && _data[3] != ControllerConstant.ORDER_SBA)
					throw new ApplicationException("ni");
				Console.WriteLine("SBA");

				if (_datapos == 5)
				{
					var baddr = Util.DecodeBAddress(_data[4], _data[5]);
					Console.WriteLine(BA_TO_COL(baddr) + ", " + BA_TO_ROW(baddr));
				}

				if (_datapos > 5)
					Console.WriteLine(See.GetEbc(Tables.Cg2Ebc[_data[_datapos]]));


				_datapos++;
				break;

			case Cs.R_IAC:
				if (v == SB)
				{
					N("SB");
					_cs = Cs.R_DATA;
					_datapos = 0;
				}
				else if (v == WILL)
				{
					N("WILL");
					_cs = Cs.R_WILL;
				}
				else
				{
					NError(v);
				}

				break;
			case Cs.R_WILL:
				Console.WriteLine("will " + v);
				_cs = Cs.Waiting;
				break;
			case Cs.R_DATA:
				if (v == IAC)
				{
					_cs = Cs.R_IAC_END;
				}
				else
				{
					_data[_datapos] = v;
					_datapos++;
				}

				break;
			case Cs.R_IAC_END:
				if (v == IAC)
				{
					_data[_datapos] = v;
					_datapos++;
				}
				else
				{
					N("IAC");
					if (v == SE)
					{
						N("SE");
						N(_data, _datapos);
						_cs = Cs.Waiting;
					}
					else
					{
						NError(v);
					}
				}

				break;
			default:
				NError(v);
				break;
		}
	}

	private void N(string text)
	{
		Console.WriteLine(text);
	}

	private void NError(byte b)
	{
		throw new ApplicationException(string.Format("parse error. State is {0} and byte is {1} ({1:x2})", _cs, b));
	}

	private void N(byte[] data, int count)
	{
		for (var i = 0; i < count; i++) Console.Write("{0:x2} ", data[i]);
		Console.WriteLine();
	}
}
