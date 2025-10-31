using System.Text.RegularExpressions;
using MQR.Services.Instructions.Models.Shared;

namespace MQR.Services.Instructions.Legacy;

/// <summary>
/// Shared utilities for parsing legacy XML string values into strongly-typed values.
/// </summary>
public static class ParsingUtilities
{
    /// <summary>
    /// The XML could specify the key and timeout either as top-level properties or in a dedicated object.
    /// We decide to always consolidate everything into a dedicated object.
    /// </summary>
    public static NavigationAction? MapNavigationAction(Query.NavigationAction? legacy, string? key, string? timeout)
    {
        if (legacy is null && key is null && timeout is null)
        {
            return null;
        }
        
        var timeoutString = legacy?.NavigationTimeoutMilliseconds ?? timeout;
        var parsedTimeout = ParseTimeSpanFromMilliseconds(timeoutString);

        var keyString = legacy?.NavigationKey ?? key;
        var parsed = Enum.TryParse<KeyCommand>(keyString, out var parsedKey);

        if (!parsed)
        {
            throw new InvalidOperationException($"Could not parse navigation key {keyString} to KeyCommand enum.");
        }
        
        return new NavigationAction
        {
            NavigationKey = parsedKey,
            NavigationTimeout = parsedTimeout,
            NavigationWait = ParseTimeSpanFromMilliseconds(legacy?.NavigationWaitMilliseconds),
            ScreenRefreshes = ParseInt(legacy?.ScreenRefreshes)
        };
    }
    
    /// <summary>
    /// The XML could specify the key and timeout either as top-level properties or in a dedicated object.
    /// We decide to always consolidate everything into a dedicated object.
    /// </summary>
    public static NavigationAction? MapNavigationAction(Logon.NavigationAction? legacy, string? key, string? timeout)
    {
        if (legacy is null && key is null && timeout is null)
        {
            return null;
        }
        
        var timeoutString = legacy?.NavigationTimeoutMilliseconds ?? timeout;
        var parsedTimeout = ParseTimeSpanFromMilliseconds(timeoutString);

        var keyString = legacy?.NavigationKey ?? key;
        var parsed = Enum.TryParse<KeyCommand>(keyString, out var parsedKey);

        if (!parsed)
        {
            throw new InvalidOperationException($"Could not parse navigation key {keyString} to KeyCommand enum.");
        }
        
        return new NavigationAction
        {
            NavigationKey = parsedKey,
            NavigationTimeout = parsedTimeout,
            NavigationWait = ParseTimeSpanFromMilliseconds(legacy?.NavigationWaitMilliseconds),
            ScreenRefreshes = ParseInt(legacy?.ScreenRefreshes)
        };
    }
    
    /// <summary>
    /// Returns null if the string is null, empty, or whitespace.
    /// </summary>
    public static string? NullIfEmpty(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    /// <summary>
    /// Parses a string to an int. Returns null if the string is empty, whitespace, or "-1".
    /// </summary>
    public static int? ParseInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (value == "-1")
        {
            return null;
        }

        return int.TryParse(value, out var result) ? result : null;
    }
    
    public static uint? ParseUnsignedInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (value == "-1")
        {
            return null;
        }

        return uint.TryParse(value, out var result) ? result : null;
    }

    /// <summary>
    /// Parses a string to a bool. Returns null if the string is empty or whitespace.
    /// Non-present values should be treated as false by the caller.
    /// </summary>
    public static bool ParseBool(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return bool.TryParse(value, out var result) && result;
    }

    /// <summary>
    /// Parses a string to a DateTime. Returns null if the string is empty, whitespace,
    /// or represents a date before 1990 (which indicates a sentinel/default value).
    /// </summary>
    public static DateTime? ParseDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var ok = DateTime.TryParse(value, out var result);

        if (!ok)
        {
            return null;
        }

        // Some sets have default 0001 style year values, just turn them null.
        if (result.Year < 1990)
        {
            return null;
        }

        return result;
    }

    /// <summary>
    /// Parses a string to a DateTimeOffset. Returns null if the string is empty, whitespace,
    /// or represents a date before 1990 (which indicates a sentinel/default value).
    /// </summary>
    public static DateTimeOffset? ParseDateTimeOffset(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var ok = DateTimeOffset.TryParse(value, out var result);

        if (!ok)
        {
            return null;
        }

        // Some sets have default 0001 style year values, just turn them null.
        if (result.Year < 1990)
        {
            return null;
        }

        return result;
    }

    /// <summary>
    /// Parses a TimeSpan string (e.g., "00:05:00"). Returns null if the string is empty,
    /// whitespace, or negative (which indicates a sentinel value like -00:01:00).
    /// </summary>
    public static TimeSpan? ParseTimeSpan(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var ok = TimeSpan.TryParse(value, out var result);

        if (!ok)
        {
            return null;
        }

        // Some sets use negative values as a sentinel.
        if (result.TotalMilliseconds < 0)
        {
            return null;
        }

        return result;
    }

    /// <summary>
    /// Parses a string representing milliseconds into a TimeSpan.
    /// Returns null if the string is empty, whitespace, or negative.
    /// </summary>
    public static TimeSpan? ParseTimeSpanFromMilliseconds(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!int.TryParse(value, out var milliseconds))
        {
            return null;
        }

        if (milliseconds < 0)
        {
            return null;
        }

        return TimeSpan.FromMilliseconds(milliseconds);
    }
    
    public static TimeSpan? ParseTimeSpanFromSeconds(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!int.TryParse(value, out var seconds))
        {
            return null;
        }

        if (seconds < 0)
        {
            return null;
        }

        return TimeSpan.FromSeconds(seconds);
    }

    /// <summary>
    /// Parses a string representing minutes into a TimeSpan.
    /// Returns null if the string is empty, whitespace, or negative.
    /// </summary>
    public static TimeSpan? ParseTimeSpanFromMinutes(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!double.TryParse(value, out var minutes))
        {
            return null;
        }

        if (minutes < 0)
        {
            return null;
        }

        return TimeSpan.FromMinutes(minutes);
    }

    /// <summary>
    /// Parses a string representing RegexOptions (e.g., "IgnoreCase").
    /// Returns RegexOptions.None if the string is empty or cannot be parsed.
    /// </summary>
    public static RegexOptions ParseRegexOptions(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return RegexOptions.None;
        }

        if (Enum.TryParse<RegexOptions>(value, true, out var result))
        {
            return result;
        }

        return RegexOptions.None;
    }
}
