using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Open3270.Library;

namespace Open3270;

[XmlRoot("XMLScreen")]
public partial class TnEmulator
{
	public bool WaitForRegex(Func<string> getScreenData, string regExPattern, RegexOptions regExOptions, int timeoutMs)
	{
		var regex = new Regex(regExPattern, regExOptions);
		if (_currentConnection == null) throw new TnHostException("TNEmulator is not connected", "There is no currently open TN3270 connection", null);
		var start = DateTime.Now.Ticks;
		do
		{
			if (CurrentScreen != null)
			{
				var screenText = getScreenData();
				
				//if (screenText == text)
				if (regex.IsMatch(screenText))
				{
					Audit?.WriteLine($"WaitForRegex('{regExPattern}') found!");
					return true;
				}
				
				Audit?.WriteLine($"WaitForRegex('{regExPattern}') not found on screen.");
			}
			
			if (timeoutMs == 0)
			{
				return false;
			}
			
			if (Config.AlwaysRefreshWhenWaiting)
			{
				lock (this)
				{
					DisposeOfCurrentScreenXml();
				}
			}

			Refresh(true, 1000);
		} while ((DateTime.Now.Ticks - start) / 10000 < timeoutMs);
		
		Audit?.WriteLine($"WaitForRegex('{regExPattern}') Timed out");
		return false;
	}

	public void WriteAudit(string value)
	{
		Audit?.WriteLine(value);
	}
}
