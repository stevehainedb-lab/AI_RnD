namespace MQR.Services.Instructions.Legacy.Parsing;

// Auto-generated POCOs for Parse Instruction Set files
// Attributes are strings for safe XmlSerializer behavior.
using System.Xml.Serialization;
using System.Collections.Generic;

[XmlType("ParseInstructionSet")]
[XmlRoot("ParseInstructionSet")]
public class ParseInstructionSet
{
    [XmlAttribute("CaptureRaw")]
    public string CaptureRaw { get; set; }

    [XmlAttribute("Identifier")]
    public string Identifier { get; set; }

    [XmlAttribute("IncludeDuplicatesForValidation")]
    public string IncludeDuplicatesForValidation { get; set; }

    [XmlElement("EndOfDataIdentification")]
    public EndOfDataIdentification? EndOfDataIdentification { get; set; }

    [XmlElement("ErrorIdentification")]
    public List<ErrorIdentification> ErrorIdentification { get; set; } = [];

    [XmlElement("ParseOption")]
    public List<ParseOption> ParseOption { get; set; } = [];

    [XmlElement("ValidationCountIdentificationMark")]
    public List<ValidationCountIdentificationMark> ValidationCountIdentificationMark { get; set; } = [];

}

[XmlType("ErrorIdentification")]
public class ErrorIdentification
{
    [XmlAttribute("Identifier")]
    public string Identifier { get; set; }

    [XmlAttribute("MakePoolUnavailable")]
    public string MakePoolUnavailable { get; set; }

    [XmlAttribute("PoolUnavailablePeriod")]
    public string PoolUnavailablePeriod { get; set; }

    [XmlElement("RegExPattern")]
    public RegExPattern RegExPattern { get; set; }

    [XmlElement("ScreenArea")]
    public ScreenArea ScreenArea { get; set; }

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

[XmlType("ParseOption")]
public class ParseOption
{
    [XmlAttribute("Identifier")]
    public string? Identifier { get; set; }

    [XmlElement("ParseCategory")]
    public List<ParseCategory> ParseCategory { get; set; } = [];

    [XmlElement("ParseOptionIdentifer")]
    public List<ParseOptionIdentifer> ParseOptionIdentifer { get; set; } = [];

}

[XmlType("ParseCategory")]
public class ParseCategory
{
    [XmlAttribute("Identifier")]
    public string Identifier { get; set; }

    [XmlAttribute("SubRowSearchLineCount")]
    public string SubRowSearchLineCount { get; set; }

    [XmlElement("FieldDefintion")]
    public List<FieldDefintion> FieldDefintion { get; set; } = [];

    [XmlElement("KeyDefintion")]
    public List<KeyDefintion> KeyDefintion { get; set; } = [];

    [XmlElement("RowIdentification")]
    public List<RowIdentification> RowIdentification { get; set; } = [];

    [XmlElement("SubRowIdentification")]
    public List<SubRowIdentification> SubRowIdentification { get; set; } = [];

}

[XmlType("RowIdentification")]
public class RowIdentification
{
    [XmlAttribute("Identifier")]
    public string Identifier { get; set; }

    [XmlAttribute("Length")]
    public string Length { get; set; }

    [XmlAttribute("XPos")]
    public string XPos { get; set; }

    [XmlElement("RegExPattern")]
    public RegExPattern RegExPattern { get; set; }

}

[XmlType("FieldDefintion")]
public class FieldDefintion
{
    [XmlAttribute("Identifier")]
    public string Identifier { get; set; }

    [XmlAttribute("Length")]
    public string Length { get; set; }

    [XmlAttribute("XPos")]
    public string XPos { get; set; }

    [XmlElement("RegExPattern")]
    public RegExPattern RegExPattern { get; set; }

}

[XmlType("EndOfDataIdentification")]
public class EndOfDataIdentification
{
    [XmlAttribute("ExceptIfNotFound")]
    public string ExceptIfNotFound { get; set; }

    [XmlAttribute("Identifier")]
    public string Identifier { get; set; }

    [XmlElement("RegExPattern")]
    public RegExPattern RegExPattern { get; set; }

    [XmlElement("ScreenArea")]
    public ScreenArea ScreenArea { get; set; }

}

[XmlType("KeyDefintion")]
public class KeyDefintion
{
    [XmlAttribute("Identifier")]
    public string Identifier { get; set; }

    [XmlAttribute("Length")]
    public string Length { get; set; }

    [XmlAttribute("XPos")]
    public string XPos { get; set; }

    [XmlElement("RegExPattern")]
    public RegExPattern RegExPattern { get; set; }

}

[XmlType("ValidationCountIdentificationMark")]
public class ValidationCountIdentificationMark
{
    [XmlAttribute("Identifier")]
    public string Identifier { get; set; }

    [XmlAttribute("Length")]
    public string Length { get; set; }

    [XmlAttribute("XPos")]
    public string XPos { get; set; }

    [XmlElement("RegExPattern")]
    public RegExPattern RegExPattern { get; set; }

}

[XmlType("ParseOptionIdentifer")]
public class ParseOptionIdentifer
{
    [XmlAttribute("Identifier")]
    public string Identifier { get; set; }

    [XmlElement("RegExPattern")]
    public RegExPattern RegExPattern { get; set; }

    [XmlElement("ScreenArea")]
    public ScreenArea ScreenArea { get; set; }

}

[XmlType("SubRowIdentification")]
public class SubRowIdentification
{
    [XmlAttribute("Identifier")]
    public string Identifier { get; set; }

    [XmlAttribute("Length")]
    public string Length { get; set; }

    [XmlAttribute("XPos")]
    public string XPos { get; set; }

    [XmlElement("FieldDefintion")]
    public required FieldDefintion FieldDefintion { get; set; } 
    [XmlElement("RegExPattern")]
    public required RegExPattern RegExPattern { get; set; }

}
