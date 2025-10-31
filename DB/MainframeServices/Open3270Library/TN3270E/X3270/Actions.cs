#region License

/*
 *
 * Open3270 - A C# implementation of the TN3270/TN3270E protocol
 *
 * Copyright (c) 2004-2020 Michael Warriner
 * Modifications (c) as per Git change history
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software
 * and associated documentation files (the "Software"), to deal in the Software without restriction,
 * including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
 * subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or substantial
 * portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT
 * LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

#endregion

using System;
using System.Collections;
using System.Text;

namespace Open3270.TN3270;

internal delegate bool ActionDelegate(params object[] args);

internal class Actions
{
	private readonly Hashtable _actionLookup = new();
	private readonly XtActionRec[] _actions;
	private ArrayList _datacapture;
	private ArrayList _datastringcapture;

	//public iaction ia_cause;

	public string[] IaName = new[]
	{
		"String", "Paste", "Screen redraw", "Keypad", "Default", "Key",
		"Macro", "Script", "Peek", "Typeahead", "File transfer", "Command",
		"Keymap"
	};

	private readonly Telnet _telnet;

	internal Actions(Telnet tn)
	{
		_telnet = tn;
		_actions = new[]
		{
			new XtActionRec("printtext", false, _telnet.Print.PrintTextAction),
			new XtActionRec("flip", false, _telnet.Keyboard.FlipAction),
			new XtActionRec("ascii", false, _telnet.Controller.AsciiAction),
			new XtActionRec("dumpxml", false, _telnet.Controller.DumpXMLAction),
			new XtActionRec("asciifield", false, _telnet.Controller.AsciiFieldAction),
			new XtActionRec("attn", true, _telnet.Keyboard.AttnAction),
			new XtActionRec("backspace", false, _telnet.Keyboard.BackSpaceAction),
			new XtActionRec("backtab", false, _telnet.Keyboard.BackTab_action),
			new XtActionRec("circumnot", false, _telnet.Keyboard.CircumNotAction),
			new XtActionRec("clear", true, _telnet.Keyboard.ClearAction),
			new XtActionRec("cursorselect", false, _telnet.Keyboard.CursorSelectAction),
			new XtActionRec("delete", false, _telnet.Keyboard.DeleteAction),
			new XtActionRec("deletefield", false, _telnet.Keyboard.DeleteFieldAction),
			new XtActionRec("deleteword", false, _telnet.Keyboard.DeleteWordAction),
			new XtActionRec("down", false, _telnet.Keyboard.MoveCursorDown),
			new XtActionRec("dup", false, _telnet.Keyboard.DupAction),
			new XtActionRec("emulateinput", true, _telnet.Keyboard.EmulateInputAction),
			new XtActionRec("enter", true, _telnet.Keyboard.EnterAction),
			new XtActionRec("erase", false, _telnet.Keyboard.EraseAction),
			new XtActionRec("eraseeof", false, _telnet.Keyboard.EraseEndOfFieldAction),
			new XtActionRec("eraseinput", false, _telnet.Keyboard.EraseInputAction),
			new XtActionRec("fieldend", false, _telnet.Keyboard.FieldEndAction),
			new XtActionRec("fields", false, _telnet.Keyboard.FieldsAction),
			new XtActionRec("fieldget", false, _telnet.Keyboard.FieldGetAction),
			new XtActionRec("fieldset", false, _telnet.Keyboard.FieldSetAction),
			new XtActionRec("fieldmark", false, _telnet.Keyboard.FieldMarkAction),
			new XtActionRec("fieldexit", false, _telnet.Keyboard.FieldExitAction),
			new XtActionRec("hexString", false, _telnet.Keyboard.HexStringAction),
			new XtActionRec("home", false, _telnet.Keyboard.HomeAction),
			new XtActionRec("insert", false, _telnet.Keyboard.InsertAction),
			new XtActionRec("interrupt", true, _telnet.Keyboard.InterruptAction),
			new XtActionRec("key", false, _telnet.Keyboard.SendKeyAction),
			new XtActionRec("left", false, _telnet.Keyboard.LeftAction),
			new XtActionRec("left2", false, _telnet.Keyboard.MoveCursorLeft2Positions),
			new XtActionRec("monocase", false, _telnet.Keyboard.MonoCaseAction),
			new XtActionRec("movecursor", false, _telnet.Keyboard.MoveCursorAction),
			new XtActionRec("Newline", false, _telnet.Keyboard.MoveCursorToNewLine),
			new XtActionRec("NextWord", false, _telnet.Keyboard.MoveCursorToNextUnprotectedWord),
			new XtActionRec("PA", true, _telnet.Keyboard.PAAction),
			new XtActionRec("PF", true, _telnet.Keyboard.PFAction),
			new XtActionRec("PreviousWord", false, _telnet.Keyboard.PreviousWordAction),
			new XtActionRec("Reset", true, _telnet.Keyboard.ResetAction),
			new XtActionRec("Right", false, _telnet.Keyboard.MoveRight),
			new XtActionRec("Right2", false, _telnet.Keyboard.MoveCursorRight2Positions),
			new XtActionRec("String", true, _telnet.Keyboard.SendStringAction),
			new XtActionRec("SysReq", true, _telnet.Keyboard.SystemRequestAction),
			new XtActionRec("Tab", false, _telnet.Keyboard.TabForwardAction),
			new XtActionRec("ToggleInsert", false, _telnet.Keyboard.ToggleInsertAction),
			new XtActionRec("ToggleReverse", false, _telnet.Keyboard.ToggleReverseAction),
			new XtActionRec("Up", false, _telnet.Keyboard.MoveCursorUp)
		};
	}
	
	/*
	 * Wrapper for calling an action internally.
	 */
	public bool action_internal(ActionDelegate action, params object[] args)
	{
		return action(args);
	}

	public void action_output(string data)
	{
		action_output(data, false);
	}

	private string EncodeXml(string data)
	{
		//data = data.Replace("\"", "&quot;");
		//data = data.Replace(">", "&gt;");
		data = data.Replace("<", "&lt;");
		data = data.Replace("&", "&amp;");
		return data;
	}

	public void action_output(string data, bool encode)
	{
		if (_datacapture == null)
			_datacapture = new ArrayList();
		if (_datastringcapture == null)
			_datastringcapture = new ArrayList();

		_datacapture.Add(Encoding.ASCII.GetBytes(data));
		//
		if (encode) data = EncodeXml(data);
		//
		_datastringcapture.Add(data);
	}

	public void action_output(byte[] data, int length)
	{
		action_output(data, length, false);
	}

	public void action_output(byte[] data, int length, bool encode)
	{
		if (_datacapture == null)
			_datacapture = new ArrayList();
		if (_datastringcapture == null)
			_datastringcapture = new ArrayList();

		//
		var temp = new byte[length];
		int i;
		for (i = 0; i < length; i++) temp[i] = data[i];
		_datacapture.Add(temp);
		var strdata = Encoding.ASCII.GetString(temp);
		if (encode) strdata = EncodeXml(strdata);

		_datastringcapture.Add(strdata);
	}

	public string GetStringData(int index)
	{
		if (_datastringcapture == null)
			return null;
		if (index >= 0 && index < _datastringcapture.Count)
			return (string)_datastringcapture[index];
		return null;
	}

	public byte[] GetByteData(int index)
	{
		if (_datacapture == null)
			return null;
		if (index >= 0 && index < _datacapture.Count)
			return (byte[])_datacapture[index];
		return null;
	}

	public bool KeyboardCommandCausesSubmit(string name)
	{
		var rec = _actionLookup[name.ToLower()] as XtActionRec;
		if (rec != null) return rec.CausesSubmit;

		for (var i = 0; i < _actions.Length; i++)
			if (_actions[i].Name.ToLower() == name.ToLower())
			{
				_actionLookup[name.ToLower()] = _actions[i];
				return _actions[i].CausesSubmit;
			}

		throw new ApplicationException("Sorry, action '" + name + "' is not known");
	}

	public bool Execute(bool submit, string name, params object[] args)
	{
		_telnet.Events.Clear();
		// Check that we're connected
		if (!_telnet.IsConnected) throw new TnHostException("TN3270 Host is not connected", _telnet.DisconnectReason, null);

		_datacapture = null;
		_datastringcapture = null;
		var rec = _actionLookup[name.ToLower()] as XtActionRec;
		if (rec != null) return rec.Proc(args);
		int i;
		for (i = 0; i < _actions.Length; i++)
			if (_actions[i].Name.ToLower() == name.ToLower())
			{
				_actionLookup[name.ToLower()] = _actions[i];
				return _actions[i].Proc(args);
			}

		throw new ApplicationException("Sorry, action '" + name + "' is not known");
	}


	#region Nested classes

	internal class XtActionRec
	{
		public bool CausesSubmit;
		public string Name;
		public ActionDelegate Proc;

		public XtActionRec(string name, bool causesSubmit, ActionDelegate fn)
		{
			this.CausesSubmit = causesSubmit;
			Proc = fn;
			this.Name = name.ToLower();
		}
	}

	#endregion Nested classes
}
