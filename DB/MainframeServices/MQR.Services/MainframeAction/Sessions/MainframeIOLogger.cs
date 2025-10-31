using System.Text;
using Microsoft.Extensions.Logging;
using Open3270;

namespace MQR.Services.MainframeAction;

public interface IMainframeIoLogger
{
    void LogImportantLine(string text);
    void LogCurrentScreen(string visibleScreen);
    void LogWrapped(IEnumerable<string> content, bool includeDate = true);
    void ClearCurrentTrace();
    string GetScreenTrace(string? visibleScreen = null);
}
public class MainframeIoLogger(ILogger<MainframeIoLogger> logger) : IMainframeIoLogger
{
    public const int MaxScreenWidth = 80;
    private string _lastTraceLine = string.Empty;
    private readonly StringBuilder _screenTrace = new();
    
    public void LogCurrentScreen(string visibleScreen)
    {
        LogWrapped([visibleScreen]);
    }
    
    public string GetScreenTrace(string? visibleScreen = null)
    {
        if (visibleScreen != null) Log(visibleScreen);
        return _screenTrace.ToString();
    }
    
    public void ClearCurrentTrace()
    {
        _screenTrace.Clear();
    }
    
    public void LogWrapped(IEnumerable<string> content, bool includeDate = true)
    {
        if (!logger.IsEnabled(LogLevel.Debug)) return;
        var data = new StringBuilder();
        data.AppendLine(includeDate ? PaddedLine(DateLine) : OpenCloseLine);
        foreach (var line in content)
        {
            data.AppendLine(line);    
        }
        data.Append(OpenCloseLine);
        Log(data);
    }
    
    public void LogImportantLine(string text)
    {
        if (!logger.IsEnabled(LogLevel.Debug)) return;
        Log(PaddedLine(text));
    }

    private string PaddedLine(string line)
    {
        if (line.Length > MaxScreenWidth)
        {
            return $"--- {line} ---";
        }
        var paddingWidth = (MaxScreenWidth - (line.Length+2)) / 2;
        var padding = paddingWidth > 0 ? string.Empty.PadLeft(paddingWidth, '-') : string.Empty;
        return $"{padding} {line} {padding}".PadLeft(MaxScreenWidth, '-');   
    }
    
    private static string OpenCloseLine => string.Empty.PadRight(MaxScreenWidth, '-');
    private static string DateLine => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
    
    private void Log(StringBuilder builder)
    {
        var text = builder.ToString();
        if (text == _lastTraceLine) return;
        _lastTraceLine = text;
        _screenTrace.AppendLine(text);
        logger.LogDebug("{TraceLine}", text);
    }

    private void Log(string text)
    {
        if (!logger.IsEnabled(LogLevel.Debug)) return;
        if (text == _lastTraceLine) return;
        _lastTraceLine = text;
        _screenTrace.AppendLine(text);
        logger.LogDebug("{TraceLine}", text);
    }
}
