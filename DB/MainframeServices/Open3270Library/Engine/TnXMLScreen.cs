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
using System.Security.Cryptography;
using System.Text;
using System.Xml.Schema;
using System.Xml.Serialization;
using Open3270.Internal;

namespace Open3270.TN3270;

/// <summary>
///     Do not use this class, use IXMLScreen instead...!
/// </summary>
[Serializable]
public partial class Screen : IScreen, IDisposable
{
	// CFC,Jr. 2008/07/11 initialize _CX, _CY to default values
	private bool _isDisposed;
	private bool _showFields = false;

	private char[] _mScreenBuffer;
	private string[] _mScreenRows;
	[XmlIgnore] private Guid _screenGuid;

	private string _stringValueCache;
	
	[XmlElement("Field")] public ScreenField[] Field;

	[XmlIgnore] public string MatchListIdentified;

	[XmlElement("Unformatted")] public XmlUnformattedScreen Unformatted;

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	//
	[XmlIgnore] public Guid ScreenGuid => _screenGuid;

	public ScreenField[] Fields => Field;

	[XmlIgnore] public int Cx { get; private set; } = 80;
	[XmlIgnore] public int Cy { get; private set; } = 25;

	[XmlIgnore] private string BlankRow { get; set; } = string.Empty.PadRight(80, ' ');
	
	public string Name => MatchListIdentified;

	// helper functions
	public string GetText(int x, int y, int length)
	{
		return GetText(x + y * Cx, length);
	}


	public string GetText(int offset, int length)
	{
		var screenBuffer = _mScreenBuffer;
		if (screenBuffer == null) return null;
		int i;
		var result = "";
		var maxlen = screenBuffer.Length;
		for (i = 0; i < length; i++)
			if (i + offset < maxlen)
				if (screenBuffer.Length > i + offset)
					result += screenBuffer[i + offset];

		return result;
	}

	/*
	*/
	public int LookForTextStrings(string[] text)
	{
		var buffer = new string(_mScreenBuffer);

		for (var i = 0; i < text.Length; i++)
			if (buffer.Contains(text[i]))
				return i;
		return -1;
	}

	public StringPosition LookForTextStrings2(string[] text)
	{
		var buffer = new string(_mScreenBuffer);

		for (var i = 0; i < text.Length; i++)
			if (buffer.Contains(text[i]))
			{
				var index = buffer.IndexOf(text[i], StringComparison.Ordinal);
				var s = new StringPosition
				{
					IndexInStringArray = i,
					Str = text[i],
					X = index % Cx,
					Y = index / Cx
				};
				return s;
			}

		return null;
	}

	public char GetCharAt(int offset)
	{
		return _mScreenBuffer[offset];
	}

	public string GetRow(int row)
	{
		return _mScreenRows[row];
	}

	public string Dump(bool withCoordinates = false)
	{
		var audit = new StringAudit();
		Dump(audit, withCoordinates);
		return audit.ToString();
	}

	public void Dump(IAudit stream, bool withCoordinates = false)
	{
		int i;
		if (withCoordinates)
		{
			stream.WriteLine("   ".PadRight(Cx+3, '-'));
			string tens = "   ", singles = "   "; // the quoted strings must be 3 spaces each, it gets lost in translation by codeplex...
			for (i = 0; i <= Cx; i += 10)
			{
				tens += string.Format("{0,-10}", i / 10);
				singles += "0123456789";
			}
			
			stream.WriteLine(tens.Substring(0, 3 + Cx));
			stream.WriteLine(singles.Substring(0, 3 + Cx));
		}

		for (i = 0; i <= Cy; i++)
		{
			var line = GetText(0, i, Cx);
			if (withCoordinates) line = $" {i,2}{line}";
			stream.WriteLine(line);
		}

		if (withCoordinates) stream.WriteLine("   ".PadRight(Cx+3, '-'));
	}

	public string GetXmlText()
	{
		return GetXmlText(true);
	}

	public string GetXmlText(bool useCache)
	{
		if (!useCache || _stringValueCache == null)
		{
			//
			var serializer = new XmlSerializer(typeof(Screen));
			//
			StringWriter fs = null;

			try
			{
				var builder = new StringBuilder();
				fs = new StringWriter(builder);
				serializer.Serialize(fs, this);
				fs.Close();

				_stringValueCache = builder.ToString();
			}
			finally
			{
				if (fs != null)
					fs.Close();
			}
		}

		return _stringValueCache;
	}

	~Screen()
	{
		Dispose(false);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (_isDisposed)
			return;
		_isDisposed = true;

		if (disposing)
		{
			Field = null;
			Unformatted = null;
			MatchListIdentified = null;
			_mScreenBuffer = null;
			_mScreenRows = null;
		}
	}

	public static Screen Load(Stream sr)
	{
		var serializer = new XmlSerializer(typeof(Screen));
		//
		Screen rules;

		var temp = serializer.Deserialize(sr);
		rules = (Screen)temp;

		if (rules != null)
		{
			rules.Render();
		}

		return rules;
	}

	public static Screen Load(string filename)
	{
		var serializer = new XmlSerializer(typeof(Screen));
		//
		FileStream fs = null;
		Screen rules;

		try
		{
			fs = new FileStream(filename, FileMode.Open);
			//XmlTextReader reader = new XmlTextReader(fs);
			rules = (Screen)serializer.Deserialize(fs);
		}
		finally
		{
			if (fs != null)
				fs.Close();
		}

		rules?.Render();
		return rules;
	}
	
	public void Render()
	{
		//   TO DO: REWRITE THIS CLASS! and maybe the whole process of
		//          getting data from the lower classes, why convert buffers
		//          to XML just to convert them again in this Render method?
		//  ALSO: this conversion process does not take into account that
		//        the XML data that this class is converted from might
		//        contain more than _CX characters in a line, since the
		//        previous conversion to XML converts '<' to "&lt;" and
		//        the like, which will also cause shifts in character positions.
		//
		// Reset cache
		//
		_stringValueCache = null;
		//
		if (Cx == 0 || Cy == 0)
		{
			// TODO: Need to fix this
			Cx = 132;
			Cy = 43;
		}

		// CFCJr 2008/07/11
		if (Cx < 80)
			Cx = 80;
		
		if (Cy < 25)
			Cy = 25;
		
		// CFCJr 2008/07/11
		if (Cx < 80)
			Cx = 80;
		
		if (Cy < 25)
			Cy = 25;
		
		MatchListIdentified = null;
		//
		// Render text image of screen
		//
		//
		BlankRow = string.Empty.PadRight(Cx, ' ');
		_mScreenBuffer = new char[Cx * Cy];
		_mScreenRows = new string[Cy];

		// CFCJr 2008/07/11
		// The following might be much faster:
		//
		//   string str = "".PadRight(_CX*_CY, ' ');
		//   mScreenBuffer = str.ToCharArray();
		//     ........do operations on mScreenBuffer to fill it......
		//   str = string.FromCharArray(mScreenBuffer);
		//   for (int r = 0; r < _CY; r++)
		//        mScreenRows[i] = str.SubString(r*_CY,_CX);
		//
		//  ie, fill mScreenBuffer with the data from Unformatted and Field, then
		//   create str (for the hash) and mScreenRows[]
		//   with the result.
		int i;
		for (i = 0; i < _mScreenBuffer.Length; i++) // CFCJr. 2008.07/11 replase _CX*CY with mScreenBuffer.Length
			_mScreenBuffer[i] = ' ';
		//

		int chindex;

		if (Field == null || (Field.Length == 0 &&
		                      (Unformatted == null || Unformatted.Text == null)))
		{
			//if (Unformatted == null || Unformatted.Text == null)
			//	Console.WriteLine("XMLScreen:Render: **BUGBUG** XMLScreen.Unformatted screen is blank");
			//else
			//	Console.WriteLine("XMLScreen:Render: **BUGBUG** XMLScreen.Field is blank");

			Console.Out.Flush();

			// CFCJr. Move logic for what is in mScreenRows to seperate if logic
			//        this will give unformatted results even if Field==null or 0 length
			//        and vise-a-versa.
			/*
			for (i=0; i<mScreenRows.Length; i++)
			{
				mScreenRows[i] = new String(' ',_CX);
			}
			*/
		}

		if (Unformatted == null || Unformatted.Text == null)
			// CFCJr. 2008/07/11 initilize a blank row of _CX (80?) spaces
			for (i = 0; i < _mScreenRows.Length; i++)
				//mScreenRows[i] = "                                                                                              ".Substring(0, _CX);
				// CFCJr. 2008/07/11 replace above method of 80 spaces with following
				_mScreenRows[i] = BlankRow;
		else
			for (i = 0; i < Unformatted.Text.Length; i++)
			{
				var text = Unformatted.Text[i];

				// CFCJr, make sure text is not null
				if (string.IsNullOrEmpty(text))
					text = string.Empty;

				// CFCJr, replace "&lt;" with '<'
				text = text.Replace("&lt;", "<");

				// CFCJr, Remove this loop to pad text
				// and use text.PadRight later.
				// This will help in not processing more
				// characters than necessary into mScreenBuffer
				// below
				//while (text.Length < _CX)
				//	text+=" ";
				//
				int p;
				//for (p=0; p<_CX; p++)
				for (p = 0; p < text.Length; p++) // CFC,Jr.
					if (text[p] < 32 || text[p] > 126)
						text = text.Replace(text[p], ' ');

				//
				//for (chindex=0; chindex<Unformatted.Text[i].Length; chindex++)
				// CFCJr, 2008/07/11 use text.length instead of Unformatted.Text[i].Length
				// since we only pad text with 80 chars but if Unformatted.Text[i]
				// contains XML codes (ie, "&lt;") then it could be longer than
				// 80 chars (hence, longer than text). 
				// Also, I replace "&lt;" above with "<".
				for (chindex = 0; chindex < text.Length; chindex++)
				{
					// CFCJr, calculate mScreenBuffer index only once
					var bufNdx = chindex + i * Cx;

					if (bufNdx < _mScreenBuffer.Length) 
						_mScreenBuffer[bufNdx] = text[chindex];
				}

				// CFCJr, make sure we don't overflow the index of mScreenRows
				//        since i is based on the dimensions of Unformatted.Text
				//        instead of mScreenRows.Length
				if (i < _mScreenRows.Length)
				{
					text = text.PadRight(Cx, ' '); // CFCJr. 2008/07/11 use PadRight instead of loop above
					_mScreenRows[i] = text;
				}
			}

		// CFCJr, lets make sure we have _CY rows in mScreenRows here
		// since we use Unformated.Text.Length for loop above which
		// could possibly be less than _CY.

		for (i = 0; i < _mScreenRows.Length; i++)
			if (string.IsNullOrEmpty(_mScreenRows[i]))
				_mScreenRows[i] = BlankRow;

		//==============
		// Now process the Field (s)

		if (Field != null && Field.Length > 0)
		{
			//
			// Now superimpose the formatted fields on the unformatted base
			//

			if (_showFields)
			{
				for (i = 0; i < Field.Length; i++)
				{
					var field = Field[i];
					if (field.Text != null)
						for (chindex = 0; chindex < field.Text.Length; chindex++)
						{
							var ch = field.Text[chindex];
							if (ch < 32 || ch > 126)
								ch = ' ';
							// CFCJr, 2008/07/11 make sure we don't get out of bounds 
							//        of the array m_ScreenBuffer.
							var bufNdx = chindex + field.Location.Left + field.Location.Top * Cx;
							if (bufNdx >= 0 && bufNdx < _mScreenBuffer.Length)
								_mScreenBuffer[bufNdx] = ch;
						}
				}
			}

			// CFCJr, 2008/07/11
			// SOMETHING needs to be done in this method to speed things up.
			// Above, in the processing of the Unformatted.Text, Render()
			// goes to the trouble of loading up mScreenBuffer and mScreenRows.
			// now here, we replace mScreenRows with the contents of mScreenBuffer.
			// Maybe, we should only load mScreenBuffer and then at the end
			// of Render(), load mScreenRows from it (or vise-a-vera).
			// WE COULD ALSO use
			//   mScreenRows[i] = string.FromCharArraySubset(mScreenBuffer, i*_CX, _CX);
			//  inside this loop.
			for (i = 0; i < Cy; i++)
			{
				var temp = string.Empty; // CFCJr, 2008/07/11 replace ""
				for (var x = 0; x < Cx; x++)
				{
					temp += _mScreenBuffer[i * Cx + x];
				}
				_mScreenRows[i] = temp;
			}
		}

		// now calculate our screen's hash
		//
		// CFCJr, dang, now we're going to copy the data again,
		//   this time into a long string.....(see comments at top of Render())
		//   I bet there's a easy way to redo this class so that we use just
		//   one buffer (string or char[]) instead of all these buffers.
		// WE COULD also use
		//   string hashStr = string.FromCharArray(mScreenBuffer);
		// instead of converting mScreenRows to StringBuilder 
		// and then converting it to a string.

		var hash = (HashAlgorithm)CryptoConfig.CreateFromName("MD5");
		var builder = new StringBuilder();
		for (i = 0; i < _mScreenRows.Length; i++) builder.Append(_mScreenRows[i]);
		var myHash = hash?.ComputeHash(new UnicodeEncoding().GetBytes(builder.ToString()));
		if (myHash != null) BitConverter.ToString(myHash);
		_screenGuid = Guid.NewGuid();
	}

	public static Screen LoadFromString(string text)
	{
		var serializer = new XmlSerializer(typeof(Screen));
		//
		StringReader fs = null;
		Screen rules;

		try
		{
			fs = new StringReader(text);
			//XmlTextReader reader = new XmlTextReader(fs);

			rules = (Screen)serializer.Deserialize(fs); //reader);
		}
		catch (Exception e)
		{
			var dumpFileName = Path.GetTempFileName() + "_dump.xml";
			Console.WriteLine($"Exception {e.Message} reading document. Saved as {dumpFileName}");
			var sw = File.CreateText(dumpFileName);
			sw.WriteLine(text);
			sw.Close();
			throw;
		}
		finally
		{
			if (fs != null)
				fs.Close();
		}

		if (rules == null) return null;
		
		rules.Render();
		rules._stringValueCache = text;

		return rules;
	}

	public void Save(string filename)
	{
		var serializer = new XmlSerializer(typeof(Screen));
		//
		// now expand back to xml
		var fsw = new StreamWriter(filename, false, Encoding.Unicode);
		serializer.Serialize(fsw, this);
		fsw.Close();
	}
}

[Serializable]
public class XmlUnformattedScreen
{
	[XmlElement("Text")] public string[] Text;
}

[Serializable]
public class ScreenField
{
	[XmlElement("Location")] public XmlScreenLocation Location;

	[XmlElement("Attributes")] public XmlScreenAttributes Attributes;


	[XmlText] public string Text;
}

[Serializable]
public class XmlScreenLocation
{
	[XmlAttribute(Form = XmlSchemaForm.Unqualified)]
	public int Left;

	[XmlAttribute(Form = XmlSchemaForm.Unqualified)]
	public int Length;

	[XmlAttribute(Form = XmlSchemaForm.Unqualified)]
	public int Position;

	[XmlAttribute(Form = XmlSchemaForm.Unqualified)]
	public int Top;
}

[Serializable]
public class XmlScreenAttributes
{
	[XmlAttribute(Form = XmlSchemaForm.Unqualified)]
	public string Background;

	[XmlAttribute(Form = XmlSchemaForm.Unqualified)]
	public int Base;

	[XmlAttribute(Form = XmlSchemaForm.Unqualified)]
	public string FieldType;

	[XmlAttribute(Form = XmlSchemaForm.Unqualified)]
	public string Foreground;

	[XmlAttribute(Form = XmlSchemaForm.Unqualified)]
	public bool Protected;
}

//
// 
