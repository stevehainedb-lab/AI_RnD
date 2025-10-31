using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using MQR.WebAPI.ServiceModel;

namespace MQR.Services.Queues;

public partial class RawParser(ILogger<RawParser> logger)
{
    private static readonly Regex LineSplitRegex = CreateLineSplitRegex();
    
    [GeneratedRegex(@"\r\n|\r|\n|$|\f")]
    private static partial Regex CreateLineSplitRegex();

    /// <summary>
    /// Parses data in raw mode (just captures lines as-is).
    /// Returns a section containing the raw lines.
    /// </summary>
    public QueryResultSection ParseRawData(string data)
    {
        var lines = LineSplitRegex.Split(data);

        logger.LogDebug("Parsing {LineCount} rows in raw mode", lines.Length);

        return new QueryResultSection
        {
            Identifier = "RawResult",
            Rows = lines.Select((line, index) => new QueryResultRow
            {
                Identifier = (index + 1).ToString(),
                FullLine = line,
                Fields = []
            }).ToArray()
        };
    }
}