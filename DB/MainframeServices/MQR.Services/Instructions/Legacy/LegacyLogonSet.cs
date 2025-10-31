namespace MQR.Services.Instructions.Legacy.Logon;

// Auto-generated POCOs for Logon Instruction Set files
// Attributes are strings to avoid XmlSerializer issues with Nullable<T> on attributes.
using System.Xml.Serialization;
using System.Collections.Generic;

[XmlType("LogonInstructionSet")]
[XmlRoot("LogonInstructionSet")]
public class LegacyLogonInstructionSet
{
    [XmlAttribute("CredentialPool")]
    public string CredentialPool { get; set; }

    [XmlAttribute("Identifier")]
    public string Identifier { get; set; }

    [XmlElement("LogonConnection")]
    public LogonConnection LogonConnection { get; set; } = new();

    [XmlElement("LogonInstruction")]
    public LogonInstruction LogonInstruction { get; set; } = new();

    [XmlElement("LogonSession")]
    public List<LogonSession> LogonSession { get; set; } = [];

    [XmlElement("LogonStaticConfiguration")]
    public LogonStaticConfiguration LogonStaticConfiguration { get; set; } = new();

}

[XmlType("LogonConnection")]
public class LogonConnection
{
    [XmlAttribute("ConnectionTimeOut")]
    public string ConnectionTimeOut { get; set; }

    [XmlAttribute("HostAddress")]
    public string HostAddress { get; set; }

    [XmlAttribute("HostPort")]
    public string HostPort { get; set; }

    [XmlAttribute("TerminalDeviceType")]
    public string TerminalDeviceType { get; set; }

}

[XmlType("LogonStaticConfiguration")]
public class LogonStaticConfiguration
{
    [XmlAttribute("CompatableQueryList")]
    public string CompatableQueryList { get; set; }

    [XmlAttribute("ConnectionCount")]
    public string ConnectionCount { get; set; }

    [XmlAttribute("MaxConnections")]
    public string MaxConnections { get; set; }

    [XmlAttribute("MinConnections")]
    public string MinConnections { get; set; }

}

[XmlType("LogonSession")]
public class LogonSession
{
    [XmlAttribute("Available")]
    public string Available { get; set; }

    [XmlAttribute("LastUsed")]
    public string LastUsed { get; set; }

    [XmlAttribute("Priority")]
    public string Priority { get; set; }

    [XmlAttribute("SessionID")]
    public string SessionID { get; set; }

    [XmlAttribute("TimeOutCount")]
    public string TimeOutCount { get; set; }

    [XmlAttribute("UnavailabitySetTime")]
    public string UnavailabitySetTime { get; set; }

    [XmlAttribute("UnavailableReason")]
    public string UnavailableReason { get; set; }

    [XmlElement("PrinterRetryDataItem")]
    public List<PrinterRetryDataItem> PrinterRetryDataItem { get; set; } = [];

}

[XmlType("LogonInstruction")]
public class LogonInstruction
{
    [XmlAttribute("CommunicationsUnavailableRetryInterval")]
    public string CommunicationsUnavailableRetryInterval { get; set; }

    [XmlAttribute("NavigationKey")]
    public string NavigationKey { get; set; }

    [XmlAttribute("NavigationTimeout")]
    public string NavigationTimeout { get; set; }

    [XmlElement("CommunicationsUnavailableIdentificationMark")]
    public CommunicationsUnavailableIdentificationMark? CommunicationsUnavailableIdentificationMark { get; set; } = new();

    [XmlElement("LogonPasswordChange")]
    public List<LogonPasswordChange> LogonPasswordChange { get; set; } = [];

    [XmlElement("MFDateTimeCaptureDataPoint")]
    public List<MFDateTimeCaptureDataPoint> MFDateTimeCaptureDataPoint { get; set; } = [];

    [XmlElement("NavigationAction")]
    public NavigationAction? NavigationAction { get; set; }

    [XmlElement("ProcessAction")]
    public List<ProcessAction> ProcessAction { get; set; } = [];

    [XmlElement("ResetPrinterProcess")]
    public ResetPrinterProcess? ResetPrinterProcess { get; set; }

    [XmlElement("ScreenIdentificationMark")]
    public List<ScreenIdentificationMark> ScreenIdentificationMark { get; set; } = [];

    [XmlElement("ScreenInput")]
    public List<ScreenInput> ScreenInput { get; set; } = [];

    [XmlElement("SuccessCondition")]
    public SuccessCondition? SuccessCondition { get; set; }

}

[XmlType("CommunicationsUnavailableIdentificationMark")]
public class CommunicationsUnavailableIdentificationMark
{
    [XmlAttribute("Identifier")]
    public string Identifier { get; set; }

    [XmlAttribute("WaitPeriod")]
    public string WaitPeriod { get; set; }

    [XmlElement("RegExPattern")]
    public List<RegExPattern> RegExPattern { get; set; } = [];

    [XmlElement("ScreenArea")]
    public List<ScreenArea> ScreenArea { get; set; } = [];

}

[XmlType("ScreenArea")]
public class ScreenArea
{
    [XmlAttribute("EndCol")]
    public string EndCol { get; set; }

    [XmlAttribute("EndRow")]
    public string EndRow { get; set; }

    [XmlAttribute("StartAtBottom")]
    public string StartAtBottom { get; set; }

    [XmlAttribute("StartCol")]
    public string StartCol { get; set; }

    [XmlAttribute("StartRow")]
    public string StartRow { get; set; }

}

[XmlType("RegExPattern")]
public class RegExPattern
{
    [XmlAttribute("RegexOptions")]
    public string RegexOptions { get; set; }

    [XmlText]
    public string Value { get; set; }

}

[XmlType("ScreenIdentificationMark")]
public class ScreenIdentificationMark
{
    [XmlAttribute("Identifier")]
    public string Identifier { get; set; }

    [XmlAttribute("WaitPeriod")]
    public string WaitPeriod { get; set; }

    [XmlElement("RegExPattern")]
    public List<RegExPattern> RegExPattern { get; set; } = [];

    [XmlElement("ScreenArea")]
    public List<ScreenArea> ScreenArea { get; set; } = [];

}

[XmlType("MFDateTimeCaptureDataPoint")]
public class MFDateTimeCaptureDataPoint
{
    [XmlAttribute("ExceptIfNotFound")]
    public string ExceptIfNotFound { get; set; }

    [XmlAttribute("Identifier")]
    public string Identifier { get; set; }

    [XmlElement("RegExPattern")]
    public List<RegExPattern> RegExPattern { get; set; } = [];

    [XmlElement("ScreenArea")]
    public List<ScreenArea> ScreenArea { get; set; } = [];

}

[XmlType("ScreenInput")]
public class ScreenInput
{
    [XmlAttribute("FieldNumber")]
    public string FieldNumber { get; set; }

    [XmlAttribute("Identifier")]
    public string Identifier { get; set; }

    [XmlAttribute("Value")]
    public string Value { get; set; }

    [XmlAttribute("XPos")]
    public string XPos { get; set; }

    [XmlAttribute("YPos")]
    public string YPos { get; set; }

}

[XmlType("LogonPasswordChange")]
public class LogonPasswordChange
{
    [XmlElement("ProcessAction")]
    public List<ProcessAction> ProcessAction { get; set; } = [];

    [XmlElement("ScreenInput")]
    public List<ScreenInput> ScreenInput { get; set; } = [];

}

[XmlType("ProcessAction")]
public class ProcessAction
{
    [XmlAttribute("Identifier")]
    public string Identifier { get; set; }

    [XmlAttribute("IsCoreAction")]
    public string IsCoreAction { get; set; }

    [XmlAttribute("LockCredential")]
    public string LockCredential { get; set; }

    [XmlAttribute("NavigationKey")]
    public string NavigationKey { get; set; }

    [XmlAttribute("NavigationTimeout")]
    public string NavigationTimeout { get; set; }

    [XmlAttribute("NoData")]
    public string NoData { get; set; }

    [XmlAttribute("ParseScreen")]
    public string ParseScreen { get; set; }

    [XmlElement("ErrorScreenIdentificationMark")]
    public List<ErrorScreenIdentificationMark> ErrorScreenIdentificationMark { get; set; } = [];

    [XmlElement("NavigationAction")]
    public List<NavigationAction> NavigationAction { get; set; } = [];

    [XmlElement("ProcessAction")]
    public List<ProcessAction> ProcessActions { get; set; } = [];

    [XmlElement("ScreenCaptureDataPoint")]
    public List<ScreenCaptureDataPoint> ScreenCaptureDataPoint { get; set; } = [];

    [XmlElement("ScreenIdentificationMark")]
    public List<ScreenIdentificationMark> ScreenIdentificationMark { get; set; } = [];

    [XmlElement("ScreenInput")]
    public List<ScreenInput> ScreenInput { get; set; } = [];

}

[XmlType("ErrorScreenIdentificationMark")]
public class ErrorScreenIdentificationMark
{
    [XmlAttribute("ExceptIfNotFound")]
    public string ExceptIfNotFound { get; set; }

    [XmlAttribute("Identifier")]
    public string Identifier { get; set; }

    [XmlAttribute("MakePoolUnavailable")]
    public string MakePoolUnavailable { get; set; }

    [XmlAttribute("PoolUnavailablePeriod")]
    public string PoolUnavailablePeriod { get; set; }

    [XmlElement("RegExPattern")]
    public List<RegExPattern> RegExPattern { get; set; } = [];

    [XmlElement("ScreenArea")]
    public List<ScreenArea> ScreenArea { get; set; } = [];

}

[XmlType("ScreenCaptureDataPoint")]
public class ScreenCaptureDataPoint
{
    [XmlAttribute("ExceptIfNotFound")]
    public string ExceptIfNotFound { get; set; }

    [XmlAttribute("Identifier")]
    public string Identifier { get; set; }

    [XmlElement("RegExPattern")]
    public List<RegExPattern> RegExPattern { get; set; } = [];

    [XmlElement("ScreenArea")]
    public List<ScreenArea> ScreenArea { get; set; } = [];

}

[XmlType("SuccessCondition")]
public class SuccessCondition
{
    [XmlAttribute("LockCredentialOnFailure")]
    public string LockCredentialOnFailure { get; set; }

    [XmlAttribute("ResetSessionOnFailure")]
    public string ResetSessionOnFailure { get; set; }

    [XmlElement("ScreenIdentificationMark")]
    public List<ScreenIdentificationMark> ScreenIdentificationMark { get; set; } = [];

}

[XmlType("ResetPrinterProcess")]
public class ResetPrinterProcess
{
    [XmlAttribute("ActivePrinterState")]
    public string ActivePrinterState { get; set; }

    [XmlAttribute("AttemptsBeforeFailure")]
    public string AttemptsBeforeFailure { get; set; }

    [XmlAttribute("NavigationTimeout")]
    public string NavigationTimeout { get; set; }

    [XmlAttribute("ResetPeriodMins")]
    public string ResetPeriodMins { get; set; }

    [XmlElement("ResetPrinterAction")]
    public List<ResetPrinterAction> ResetPrinterAction { get; set; } = [];

}

[XmlType("ResetPrinterAction")]
public class ResetPrinterAction
{
    [XmlAttribute("AdditionalInfo")]
    public string AdditionalInfo { get; set; }

    [XmlAttribute("Type")]
    public string Type { get; set; }

    [XmlAttribute("Value")]
    public string Value { get; set; }

}

[XmlType("PrinterRetryDataItem")]
public class PrinterRetryDataItem
{
    [XmlAttribute("ResetTime")]
    public string ResetTime { get; set; }

    [XmlAttribute("State")]
    public string State { get; set; }

}

[XmlType("NavigationAction")]
public class NavigationAction
{
    [XmlAttribute("NavigationKey")]
    public string NavigationKey { get; set; }

    [XmlAttribute("NavigationTimeoutMilliseconds")]
    public string NavigationTimeoutMilliseconds { get; set; }

    [XmlAttribute("NavigationWaitMilliseconds")]
    public string NavigationWaitMilliseconds { get; set; }

    [XmlAttribute("ScreenRefreshes")]
    public string ScreenRefreshes { get; set; }

}
