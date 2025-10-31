using System;
using System.Text;

namespace Terminal
{
	/// <summary>
	/// Simple EBCDIC CP037 (U.S./U.K. English) translator that converts
	/// incoming bytes to Unicode characters for use by Tn3270eAdapter.
	/// 
	/// This implementation uses a built-in .NET Encoding for CP037.
	/// It performs zero allocations per call when the output buffer is stack-allocated.
	/// </summary>
	public sealed class EbcdicCp037Translator : ICharTranslator
	{
		// .NET includes CP037 as code page 37 — maps to EBCDIC (US/Canada/UK)
		private static readonly Encoding _encoding = Encoding.GetEncoding(37);

		/// <summary>
		/// Decode as many bytes as possible from <paramref name="input"/> into <paramref name="output"/>.
		/// Returns the number of bytes consumed. <paramref name="charsWritten"/> is the number of chars produced.
		/// </summary>
		public int Decode(ReadOnlySpan<byte> input, Span<char> output, out int charsWritten)
		{
			// Decode bytes directly into provided char buffer.
			charsWritten = _encoding.GetChars(input, output);
			return input.Length;
		}
	}
}
