using System.Xml.Serialization;

namespace MQR.Services.Instructions.Legacy.Query;

[XmlType("QueryInstructionSet")]
[XmlRoot("QueryInstructionSet")]
public class QueryInstructionSet
{
    [XmlAttribute("CompatableParseList")] public string? CompatableParseList { get; set; }

    [XmlAttribute("Identifier")] public required string Identifier { get; set; }

    [XmlAttribute("NavigationKey")] public string? NavigationKey { get; set; }

    [XmlAttribute("NavigationTimeout")] public string? NavigationTimeout { get; set; }

    [XmlElement("NavigationAction")] public NavigationAction? NavigationAction { get; set; }

    [XmlElement("ProcessAction")] public List<ProcessAction> ProcessAction { get; set; } = [];

    [XmlElement("ScreenInput")] public List<ScreenInput> ScreenInput { get; set; } = [];

    [XmlElement("SuccessCondition")] public SuccessCondition? SuccessCondition { get; set; }

    [XmlElement("SuccessNoDataCondition")]
    public SuccessNoDataCondition? SuccessNoDataCondition { get; set; }
}

[XmlType("ScreenInput")]
public class ScreenInput
{
    [XmlAttribute("FieldNumber")] public string FieldNumber { get; set; }

    [XmlAttribute("Identifier")] public required string Identifier { get; set; }

    [XmlAttribute("Value")] public string Value { get; set; }

    [XmlAttribute("XPos")] public string XPos { get; set; }

    [XmlAttribute("YPos")] public string YPos { get; set; }
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

[XmlType("ScreenIdentificationMark")]
public class ScreenIdentificationMark
{
    [XmlAttribute("Identifier")] public string Identifier { get; set; }

    [XmlAttribute("WaitPeriod")] public string WaitPeriod { get; set; }

    [XmlElement("RegExPattern")] public required RegExPattern RegExPattern { get; set; }

    [XmlElement("ScreenArea")] public required ScreenArea ScreenArea { get; set; }
}

[XmlType("ScreenArea")]
public class ScreenArea
{
    [XmlAttribute("EndCol")] public string EndCol { get; set; }

    [XmlAttribute("EndRow")] public string EndRow { get; set; }

    [XmlAttribute("StartAtBottom")] public string StartAtBottom { get; set; }

    [XmlAttribute("StartCol")] public string StartCol { get; set; }

    [XmlAttribute("StartRow")] public string StartRow { get; set; }
}

[XmlType("RegExPattern")]
public class RegExPattern
{
    [XmlAttribute("RegexOptions")] public string RegexOptions { get; set; }

    [XmlText] public required string Value { get; set; }
}

[XmlType("NavigationAction")]
public class NavigationAction
{
    [XmlAttribute("NavigationKey")] public string NavigationKey { get; set; }

    [XmlAttribute("NavigationTimeoutMilliseconds")]
    public string NavigationTimeoutMilliseconds { get; set; }

    [XmlAttribute("NavigationWaitMilliseconds")]
    public string NavigationWaitMilliseconds { get; set; }

    [XmlAttribute("ScreenRefreshes")] public string ScreenRefreshes { get; set; }
}

[XmlType("ProcessAction")]
public class ProcessAction
{
    [XmlAttribute("Identifier")] public required string Identifier { get; set; }

    [XmlAttribute("IsCoreAction")] public string IsCoreAction { get; set; }

    [XmlAttribute("LockCredential")] public string LockCredential { get; set; }

    [XmlAttribute("NavigationKey")] public string NavigationKey { get; set; }

    [XmlAttribute("NavigationTimeout")] public string NavigationTimeout { get; set; }

    [XmlAttribute("NoData")] public string NoData { get; set; }

    [XmlAttribute("ParseScreen")] public string ParseScreen { get; set; }

    [XmlElement("NavigationAction")] public NavigationAction? NavigationAction { get; set; }

    [XmlElement("ProcessAction")] public List<ProcessAction> ProcessActions { get; set; } = [];

    [XmlElement("ScreenCaptureDataPoint")]
    public List<ScreenCaptureDataPoint> ScreenCaptureDataPoint { get; set; } = [];

    [XmlElement("ScreenIdentificationMark")]
    public List<ScreenIdentificationMark> ScreenIdentificationMark { get; set; } = [];

    [XmlElement("ScreenInput")] public List<ScreenInput> ScreenInput { get; set; } = [];
}

[XmlType("ScreenCaptureDataPoint")]
public class ScreenCaptureDataPoint
{
    [XmlAttribute("ExceptIfNotFound")] public string ExceptIfNotFound { get; set; }

    [XmlAttribute("Identifier")] public required string Identifier { get; set; }

    [XmlElement("RegExPattern")] public required RegExPattern RegExPattern { get; set; }

    [XmlElement("ScreenArea")] public required ScreenArea ScreenArea { get; set; }
}

[XmlType("SuccessNoDataCondition")]
public class SuccessNoDataCondition
{
    [XmlAttribute("LockCredentialOnFailure")]
    public string LockCredentialOnFailure { get; set; }

    [XmlAttribute("ResetSessionOnFailure")]
    public string ResetSessionOnFailure { get; set; }

    [XmlElement("ScreenIdentificationMark")]
    public List<ScreenIdentificationMark> ScreenIdentificationMark { get; set; } = [];
}