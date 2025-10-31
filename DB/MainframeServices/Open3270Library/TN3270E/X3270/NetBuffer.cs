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

namespace Open3270.TN3270;

/// <summary>
///     Simple growable byte buffer optimized to avoid boxing and excessive allocations.
///     Public API preserved for compatibility.
/// </summary>
internal class NetBuffer
{
	private readonly List<byte> bytebuffer;

	internal NetBuffer()
	{
		bytebuffer = new List<byte>(128);
	}

	internal NetBuffer(byte[] data, int start, int len)
	{
		bytebuffer = new List<byte>(len);
		if (len > 0)
		{
			// Copy the requested slice
			for (var i = 0; i < len; i++)
			{
				bytebuffer.Add(data[start + i]);
			}
		}
	}

	public byte[] Data => bytebuffer.ToArray();

	public int Index => bytebuffer.Count;

	public NetBuffer CopyFrom(int start, int len)
	{
		var temp = new NetBuffer();
		if (len <= 0) return temp;
		// Bounds are assumed valid as per original usage
		for (var i = 0; i < len; i++) temp.Add(bytebuffer[start + i]);
		return temp;
	}

	public string AsString(int start, int len)
	{
		if (len <= 0) return string.Empty;
		var chars = new char[len];
		for (var i = 0; i < len; i++)
		{
			chars[i] = (char)bytebuffer[start + i];
		}
		return new string(chars);
	}

	public void Add(byte b)
	{
		bytebuffer.Add(b);
	}

	public void Add(string b)
	{
		if (string.IsNullOrEmpty(b)) return;
		for (var i = 0; i < b.Length; i++) Add((byte)b[i]);
	}

	public void Add(int b)
	{
		Add((byte)b);
	}

	public void Add(char b)
	{
		Add((byte)b);
	}

	public void IncrementAt(int index, int increment)
	{
		var v = bytebuffer[index];
		v = (byte)(v + increment);
		bytebuffer[index] = v;
	}

	public void Add16At(int index, int v16bit)
	{
		bytebuffer[index] = (byte)((v16bit & 0xFF00) >> 8);
		bytebuffer[index + 1] = (byte)(v16bit & 0x00FF);
	}

	public void Add16(int v16bit)
	{
		Add((byte)((v16bit & 0xFF00) >> 8));
		Add((byte)(v16bit & 0x00FF));
	}

	public void Add32(int v32bit)
	{
		Add((byte)((v32bit & 0xFF000000) >> 24));
		Add((byte)((v32bit & 0x00FF0000) >> 16));
		Add((byte)((v32bit & 0x0000FF00) >> 8));
		Add((byte)(v32bit & 0x000000FF));
	}

	//
	/*
	 * store3270in
	 *	Store a character in the 3270 input buffer, checking for buffer
	 *	overflow and reallocating ibuf if necessary.
	 */
	private void store3270in(byte c)
	{
		throw new ApplicationException("oops");
	}

	/*
	 * space3270out
	 *	Ensure that <n> more characters will fit in the 3270 output buffer.
	 *	Allocates the buffer in BUFSIZ chunks.
	 *	Allocates hidden space at the front of the buffer for TN3270E.
	 */
	private void space3270out(int n)
	{
		throw new ApplicationException("oops");
	}
}
