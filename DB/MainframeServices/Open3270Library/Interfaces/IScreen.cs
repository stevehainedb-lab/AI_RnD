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
using Open3270.TN3270;

namespace Open3270;

public class StringPosition
{
	public int IndexInStringArray;
	public string Str;
	public int X;
	public int Y;
}

/// <summary>
///     An interface to a 3270 Screen object. Allows you to manually manipulate a screen.
/// </summary>
public partial interface IScreen
{
	/// <summary>
	///     Returns the name of the screen as identified from the XML Connection file
	/// </summary>
	/// <value>The name of the screen</value>
	string Name { get; }

	/// <summary>
	///     Width of screen in characters
	/// </summary>
	int Cx { get; }


	/// <summary>
	///     Height of screen in characters
	/// </summary>
	int Cy { get; }

	/// <summary>
	///     Returns a unique id for the screen so you can tell whether it's changed since you last
	///     looked - doesn't necessarily mean the content has changed, just that we think it might
	///     have
	/// </summary>
	Guid ScreenGuid { get; }

	ScreenField[] Fields { get; }

	/// <summary>
	///     Returns a formatted text string representing the screen image
	/// </summary>
	/// <param name="withCoordinates"></param>
	/// <returns>The textual representation of the screen</returns>
	string Dump(bool withCoordinates = false);

	/// <summary>
	///     Streams the screen out to a TextWriter file
	/// </summary>
	/// <param name="stream">An open stream to write the screen image to</param>
	/// <param name="withCoordinates"></param>
	void Dump(IAudit stream, bool withCoordinates = false);

	/// <summary>
	///     Get text at a specified location from the screen
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="length"></param>
	/// <returns></returns>
	string GetText(int x, int y, int length);

	/// <summary>
	///     Does the text on the screen contain this text.
	/// </summary>
	/// <returns>Returns index of string that was found in the array of strings, NOT the position on the screen</returns>
	int LookForTextStrings(string[] text);

	/// <summary>
	///     Does the text on the screen contain this text.
	/// </summary>
	/// <returns>StringPoisition structure filled out for the string that was found.</returns>
	StringPosition LookForTextStrings2(string[] text);


	/// <summary>
	///     Get text at a specified 3270 offset on the screen
	/// </summary>
	/// <param name="offset"></param>
	/// <param name="length"></param>
	/// <returns></returns>
	string GetText(int offset, int length);
	
	/// <summary>
	///     Get an entire row from the screen
	/// </summary>
	/// <param name="row"></param>
	/// <returns></returns>
	string GetRow(int row);

	/// <summary>
	///     Get a character from the screen
	/// </summary>
	/// <param name="offset"></param>
	/// <returns></returns>
	char GetCharAt(int offset);

	/// <summary>
	///     Returns this screen as an XML text string
	/// </summary>
	/// <returns>XML Text for screen</returns>
	string GetXmlText(bool refreshCachedValue);

	/// <summary>
	///     Returns this screen as an XML text string. Always use the cached value (preferred option).
	/// </summary>
	/// <returns>XML Text for screen</returns>
	string GetXmlText();
	

}
