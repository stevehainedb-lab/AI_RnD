using System.Text.RegularExpressions;
using MQR.WebAPI.ServiceModel;

namespace MQR.Services.Queues;

public sealed partial class ScreenCaptureParser
{
    private static readonly Regex ScreenCaptureRegex = CreateScreenCaptureRegex();

    [GeneratedRegex(@"<(?<key>ScreenCaptureValue\w*)>(?<value>.*)</\k<key>>", RegexOptions.Compiled)]
    private static partial Regex CreateScreenCaptureRegex();

    /// <summary>
    /// Extracts screen capture values from XML-like markers in the raw data.
    /// Returns the cleaned data and an optional screen capture section.
    /// </summary>
    public (string CleanedData, QueryResultSection? ScreenCaptureSection) ProcessScreenCaptureData(string rawData)
    {
        var matches = ScreenCaptureRegex.Matches(rawData);

        if (matches.Count is 0)
        {
            return (rawData, null);
        }

        var section = CreateSectionForScreenCaptures(matches);

        // Return data with screen capture markers removed
        var cleanedData = ScreenCaptureRegex.Replace(rawData, string.Empty);
        return (cleanedData, section);
    }

    private static QueryResultSection CreateSectionForScreenCaptures(MatchCollection matches)
    {
        var fields = matches.Select(m => new QueryResultField
            {
                Identifier = m.Groups["key"].Value.Substring("ScreenCaptureValue".Length),
                Value = m.Groups["value"].Value
            })
            .ToArray();

        var row = new QueryResultRow
        {
            Identifier = "Values",
            Fields = fields
        };

        return new QueryResultSection
        {
            Identifier = "QueryScreenCapture",
            Rows = [row]
        };
    }
}