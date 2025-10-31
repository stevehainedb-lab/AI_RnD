using System.Text.RegularExpressions;
using MQR.DataAccess.Entities;
using MQR.Services.Instructions.Models.Shared;
using MQR.WebAPI.ServiceModel;
using Open3270;
using Open3270.Interfaces;

namespace MQR.Services.MainframeAction.Sessions;

public static class Extensions
{
    public static string GetText(this IScreen screen)
    {
        return screen.GetText(0, 0, screen.Cx * screen.Cy);
    }
    
    public static TnKey ToTnKey(this KeyCommand keyCommand)
    {
        return Enum.Parse<TnKey>(keyCommand.ToString().Replace("PF", "F"), true);
    }
    
    public static string GetText(this IScreen screen, ScreenArea? area)
    {
        if (area == null || area.Fullscreen)
        {
            return screen.GetText();
        }

        if (area.Field != null)
        {
            return screen.Fields[area.Field.Value].Text;
        }
        
        if (area.StartRow == null || area.StartColumn == null) 
            throw new InvalidOperationException("Screen area must have start row and start column if not full screen"); 
        
        return screen.GetText(area.StartRow.Value, area.EndRow, area.StartColumn.Value, area.EndColumn);
    }
    
    public static string GetText(this IScreen screen, ScreenArea? area, RegExPattern? pattern)
    {
        var data = screen.GetText(area);
        return pattern == null ? data : Regex.Matches(data, pattern.Pattern, pattern.RegExOptions ).First().Value;
    }
    
    public static bool IsMatch(this ScreenIdentificationMark screenIdentificationMark, string? data)
    {
        return data != null && Regex.IsMatch(data, screenIdentificationMark.RegExPattern.Pattern, screenIdentificationMark.RegExPattern.RegExOptions);
    }

    
    public const string BEGIN_VARIABLE_PLACEHOLDER = "[#";
    public const string END_VARIABLE_PLACEHOLDER = "#]";
    public const string VARIABLE_SEARCH_REGEX = @"(?<=\[#)[^\[#]*(?=\#])";

    public static string GetInputValue(this ScreenInput input, Func<string, string>? getValueMethod = null)
    {
        var result = input.Value;

        foreach (Match match in Regex.Matches(result, VARIABLE_SEARCH_REGEX))
        {
            var variableName = match.Value;
            var replaceValue = GenericGetValue(variableName, getValueMethod);

            result = result.Replace(
                BEGIN_VARIABLE_PLACEHOLDER + variableName + END_VARIABLE_PLACEHOLDER,
                replaceValue
            );
        }

        return result;
    }
    
    private static string GenericGetValue(string variableName, Func<string, string>? getValueMethod = null)
    {
        var result = string.Empty;

        if (getValueMethod != null)
        {
            result = getValueMethod.Invoke(variableName);
        }

        if (string.IsNullOrEmpty(result))
        {
            switch (variableName.ToUpperInvariant())
            {
                case "CURRENTDATETIME":
                    result = DateTime.Now.ToString("dd/MM/yy HH:mm:ss");
                    break;

                case "CURRENTTIME":
                    result = DateTime.Now.ToString("HH:mm:ss");
                    break;

                case "CURRENTDATE":
                    result = DateTime.Now.ToString("dd/MM/yy");
                    break;
            }
        }

        return result;
    }
    
    public static string GetPramValue(this QueryRequestParameter[] parameters, string parameterName)
    {
        return parameters.FirstOrDefault(x => x.Identifier.Equals(parameterName, StringComparison.InvariantCultureIgnoreCase))?.Value ?? string.Empty;
    }
    public static string ParametersString(this QueryRequest query)
    {
        return query.Parameters.ParametersString();
    }
    public static string ParametersString(this QueryRequestParameter[] parameters)
    {
        return string.Join(",", parameters.Select(x => x.Identifier + "=" + x.Value));
    }
    public static bool NeedsPasswordChanging(this LogonCredential credential, TimeSpan passwordChangeInterval)
    {
        var timeSinceChange = DateTime.UtcNow - credential.PasswordChangedDateUtc;
        return timeSinceChange > passwordChangeInterval;
    }

}