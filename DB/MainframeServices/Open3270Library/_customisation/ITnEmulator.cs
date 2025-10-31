using System;
using System.Text.RegularExpressions;

namespace Open3270.Interfaces;

public partial interface ITnEmulator
{
	IAudit Audit { get; set; }
	bool Debug { get; set; }
	ConnectionConfig Config { get;  }
	IScreen CurrentScreen { get; }
	public bool IsConnected { get; set; }

	void Connect();
	bool WaitForHostSettle(int screenCheckInterval, int finalTimeout);
	void SetCursor(int inputXPos, int inputYPos);
	void SetField(int inputFieldNumber, string valueToWrite);
	bool SendText(string valueToWrite);
	void Close();
	void Refresh();
	bool Refresh(bool b, int timeoutMs);
	bool SendKey(bool waitForScreenToUpdate, TnKey keyCommand, int timeoutMs);
	bool WaitForRegex(Func<string> getScreenData, string regExPattern, RegexOptions regExOptions, int timeoutMs);
	void WriteAudit(string text);
}
