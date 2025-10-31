using System.Text.RegularExpressions;

namespace MQR.Services.Instructions.Models.Shared;

/// <summary>
/// Regular expression pattern with options for screen matching.
/// </summary>
public class RegExPattern
{
    /// <summary>Regular expression options (e.g., IgnoreCase, Multiline).</summary>
    public RegexOptions RegExOptions { get; set; }

    /// <summary>The regex pattern string.</summary>
    public required string Pattern { get; init; }

    public override string ToString() => Pattern;
}