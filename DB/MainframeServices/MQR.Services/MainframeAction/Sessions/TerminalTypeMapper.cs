namespace MQR.Services.MainframeAction.Sessions;

public static class TerminalTypeFormatter
{
    /// <summary>
    ///     Formats terminal type strings into a hyphenated canonical form.
    ///     Examples:
    ///     IBM32782E   -> IBM-3278-2-E
    ///     32782E      -> 3278-2-E
    ///     IBM32X782E  -> IBM-32-X-782-E
    /// </summary>
    public static string Format(string input)
    {
        if (input is null) throw new ArgumentNullException(nameof(input));

        var cleaned = Normalize(input);
        if (cleaned.Length == 0) return string.Empty;

        // Split vendor (leading letters) and tail.
        var v = 0;
        while (v < cleaned.Length && char.IsLetter(cleaned[v])) v++;

        var parts = new List<string>(5);

        string tail;
        if (v > 0)
        {
            parts.Add(cleaned[..v]);
            tail = cleaned[v..];
        }
        else
        {
            // No vendor; parse whole string as tail.
            tail = cleaned;
        }

        if (tail.Length == 0)
            return string.Join("-", parts);

        var idx = 0;

        // Parse model depending on first char type.
        if (char.IsDigit(tail[0]))
        {
            // Prefer 4-digit model when possible, else all leading digits.
            var dStart = idx;
            while (idx < tail.Length && char.IsDigit(tail[idx])) idx++;
            var digitCount = idx - dStart;

            string model;
            if (digitCount >= 4)
            {
                model = tail.Substring(dStart, 4);
                // rewind residual digits after first 4
                var residualDigits = digitCount - 4;
                if (residualDigits > 0)
                    idx = dStart + 4; // leave the rest for variant/grouping
            }
            else
            {
                model = tail[dStart..idx];
            }

            if (model.Length > 0) parts.Add(model);

            // Optional variant digit.
            if (idx < tail.Length && char.IsDigit(tail[idx]))
            {
                parts.Add(tail[idx].ToString());
                idx++;
            }

            // Optional suffix letter.
            if (idx < tail.Length && char.IsLetter(tail[idx]))
            {
                parts.Add(tail[idx].ToString());
                idx++;
            }
        }
        else
        {
            // Letter-first model: take letters + following digits as one token (e.g., VT220).
            var mStart = idx;
            while (idx < tail.Length && char.IsLetter(tail[idx])) idx++;
            while (idx < tail.Length && char.IsDigit(tail[idx])) idx++;
            var model = tail[mStart..idx];
            if (model.Length > 0) parts.Add(model);

            // Optional variant digit.
            if (idx < tail.Length && char.IsDigit(tail[idx]))
            {
                parts.Add(tail[idx].ToString());
                idx++;
            }

            // Optional suffix letter.
            if (idx < tail.Length && char.IsLetter(tail[idx]))
            {
                parts.Add(tail[idx].ToString());
                idx++;
            }
        }

        // Remainder: group by runs (all digits together, all letters together).
        while (idx < tail.Length)
        {
            var start = idx;
            var isDigit = char.IsDigit(tail[idx]);
            while (idx < tail.Length && char.IsDigit(tail[idx]) == isDigit) idx++;
            parts.Add(tail[start..idx]);
        }

        return string.Join("-", parts);
    }

    private static string Normalize(string s)
    {
        Span<char> buffer = stackalloc char[s.Length];
        var j = 0;
        foreach (var ch in s)
            if (char.IsLetterOrDigit(ch))
                buffer[j++] = char.ToUpperInvariant(ch);
        return new string(buffer[..j]);
    }
}