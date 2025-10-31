using System.Text.RegularExpressions;
using MQR.Services.Instructions.Legacy.Logon;
using MQR.Services.Instructions.Models.Shared;
using static MQR.Services.Instructions.Legacy.ParsingUtilities;
using ProcessAction = MQR.Services.Instructions.Models.Shared.ProcessAction;

namespace MQR.Services.Instructions.Legacy;

/// <summary>
/// Maps legacy logon instruction sets to the modern model.
/// </summary>
public static class LogonInstructionSetMapper
{
    public static LogonInstructionSet MapToNew(LegacyLogonInstructionSet legacy)
    {
        var legacyConn = legacy.LogonConnection;

        var connection = new LogonConnection
        {
            HostAddress = legacyConn.HostAddress,
            ConnectionTimeOut = ParseTimeSpanFromMilliseconds(legacyConn.ConnectionTimeOut),
            HostPort = ParseInt(legacyConn.HostPort) ?? throw new InvalidOperationException("Could not parse port"),
            TerminalDeviceType = legacyConn.TerminalDeviceType switch
            {
                "IBM-3278-2-E" => TerminalDeviceType.IBM32782E,
                "IBM-3278-2" => TerminalDeviceType.IBM32782,
                _ => throw new ArgumentOutOfRangeException()
            },
        };

        var conf = legacy.LogonStaticConfiguration;

        // We're consolidating these all down into a single value.
        // We don't really care which we map over here, just grab whatever exists.
        var connectionCount =
            NullIfEmpty(conf.ConnectionCount) ??
            NullIfEmpty(conf.MinConnections) ??
            NullIfEmpty(conf.MaxConnections) ?? "1";

        var set = new LogonInstructionSet
        {
            Identifier = legacy.Identifier,
            CredentialPool = NullIfEmpty(legacy.CredentialPool),
            LogonConnection = connection,
            LogonInstruction = MapLogonInstruction(legacy.LogonInstruction),
            InstanceSessionCount = int.Parse(connectionCount)
        };

        return set;
    }

    private static ProcessAction MapLogonInstruction(
        MQR.Services.Instructions.Legacy.Logon.LogonInstruction? legacy)
    {
        if (legacy == null)
        {
            throw new InvalidOperationException("No logon instruction found");
        }

        var mappedMark = MapCommunicationsMark(legacy.CommunicationsUnavailableIdentificationMark);

        var marks = new List<Models.Shared.ScreenIdentificationMark>();

        if (mappedMark != null)
        {
            marks.Add(mappedMark);
        }

        return new ProcessAction
        {
            Identifier = "Logon",
            ErrorScreenIdentificationMarks = marks,
            ScreenCaptureDataPoints = legacy.MFDateTimeCaptureDataPoint.Select(MapMfDateTimeCaptureDataPoint).ToList(),
            ProcessActions = legacy.ProcessAction.Select(MapProcessAction).ToList(),
            ScreenIdentificationMarks = legacy.ScreenIdentificationMark.Select(MapScreenIdentificationMark).ToList(),
            SuccessCondition = MapSuccessCondition(legacy.SuccessCondition)
        };
    }

    private static Models.Shared.ScreenIdentificationMark?
        MapCommunicationsMark(
            MQR.Services.Instructions.Legacy.Logon.CommunicationsUnavailableIdentificationMark? legacy)
    {
        if (legacy == null)
        {
            return null;
        }

        return new Models.Shared.ScreenIdentificationMark
        {
            Identifier = NullIfEmpty(legacy.Identifier),
            WaitPeriod = ParseTimeSpan(legacy.WaitPeriod),
            RegExPattern = legacy.RegExPattern.Select(MapRegExPattern).Single(),
            ScreenArea = legacy.ScreenArea.Select(MapScreenArea).Single()
        };
    }

    private static Models.Shared.ScreenCaptureDataPoint MapMfDateTimeCaptureDataPoint(MFDateTimeCaptureDataPoint legacy)
    {
        return new Models.Shared.ScreenCaptureDataPoint
        {
            ExceptIfNotFound = ParseBool(legacy.ExceptIfNotFound),
            Identifier = legacy.Identifier ?? throw new InvalidOperationException("Identifier is required"),
            RegExPattern = legacy.RegExPattern.Select(MapRegExPattern).Single(),
            ScreenArea = legacy.ScreenArea.Select(MapScreenArea).Single()
        };
    }

    private static Models.Shared.ProcessAction MapProcessAction(
        Logon.ProcessAction legacy)
    {
        if (legacy.NavigationAction.Count > 1)
        {
            throw new InvalidOperationException("More than one navigation action found.");
        }

        var navigationAction = MapNavigationAction(
            legacy.NavigationAction.SingleOrDefault(), legacy.NavigationKey, legacy.NavigationTimeout);

        return new Models.Shared.ProcessAction
        {
            Identifier = NullIfEmpty(legacy.Identifier),
            IsCoreAction = ParseBool(legacy.IsCoreAction),
            NoPrintDataExpected = ParseBool(legacy.NoData),
            ErrorScreenIdentificationMarks = legacy.ErrorScreenIdentificationMark
                .Select(MapErrorScreenIdentificationMark)
                .ToList(),
            NavigationAction = navigationAction,
            ProcessActions = legacy.ProcessActions.Select(MapProcessAction).ToList(),
            ScreenCaptureDataPoints = legacy.ScreenCaptureDataPoint.Select(MapScreenCaptureDataPoint).ToList(),
            ScreenIdentificationMarks =
                legacy.ScreenIdentificationMark.Select(MapScreenIdentificationMark).ToList(),
            ScreenInputs = legacy.ScreenInput.Select(MapScreenInput).ToList()
        };
    }

    private static Models.Shared.ScreenIdentificationMark MapErrorScreenIdentificationMark(
        ErrorScreenIdentificationMark legacy)
    {
 
        return new Models.Shared.ScreenIdentificationMark
        {
            ExceptIfNotFound = ParseBool(legacy.ExceptIfNotFound),
            Identifier = NullIfEmpty(legacy.Identifier) ?? "[unknown error screen]",
            RegExPattern = MapRegExPattern(legacy.RegExPattern.First()),
            ScreenArea = MapScreenArea(legacy.ScreenArea.First()),
        };
    }

    private static Models.Shared.ScreenCaptureDataPoint MapScreenCaptureDataPoint(
        Logon.ScreenCaptureDataPoint legacy)
    {
        return new Models.Shared.ScreenCaptureDataPoint
        {
            ExceptIfNotFound = ParseBool(legacy.ExceptIfNotFound),
            Identifier = NullIfEmpty(legacy.Identifier),
            RegExPattern = legacy.RegExPattern.Select(MapRegExPattern).Single(),
            ScreenArea = legacy.ScreenArea.Select(MapScreenArea).Single()
        };
    }
    
    private static Models.Shared.ScreenIdentificationMark MapScreenIdentificationMark(
        Logon.ScreenIdentificationMark legacy)
    {
        return new Models.Shared.ScreenIdentificationMark
        {
            Identifier = NullIfEmpty(legacy.Identifier) ?? "[unknown screen identification mark]",
            WaitPeriod = ParseTimeSpan(legacy.WaitPeriod),
            RegExPattern = legacy.RegExPattern.Select(MapRegExPattern).Single(),
            ScreenArea = legacy.ScreenArea.Select(MapScreenArea).Single()
        };
    }

    private static Models.Shared.ScreenInput MapScreenInput(
        Logon.ScreenInput legacy)
    {
        return new Models.Shared.ScreenInput
        {
            Identifier = NullIfEmpty(legacy.Identifier) ?? "[unknown screen input]",
            Value = NullIfEmpty(legacy.Value),
            Position = new()
            {
                FieldNumber = ParseUnsignedInt(legacy.FieldNumber),
                StartColumn = ParseUnsignedInt(legacy.XPos),
                StartRow = ParseUnsignedInt(legacy.YPos) 
            }
        };
    }

    private static Models.Shared.SuccessCondition? MapSuccessCondition(
        Logon.SuccessCondition? legacy)
    {
        if (legacy == null)
        {
            return null;
        }

        return new Models.Shared.SuccessCondition
        {
            LockCredentialOnFailure = ParseBool(legacy.LockCredentialOnFailure),
            ResetSessionOnFailure = ParseBool(legacy.ResetSessionOnFailure),
            ScreenIdentificationMarks = legacy.ScreenIdentificationMark.Select(MapScreenIdentificationMark).ToList()
        };
    }

    private static Models.Shared.RegExPattern MapRegExPattern(
        Logon.RegExPattern legacy)
    {
        return new Models.Shared.RegExPattern
        {
            RegExOptions = ParseRegexOptions(legacy.RegexOptions),
            Pattern = legacy.Value
        };
    }

    private static Models.Shared.ScreenArea MapScreenArea(
        Logon.ScreenArea legacy)
    {
        var result =  new Models.Shared.ScreenArea
        {
            EndRow = ParseUnsignedInt(legacy.EndRow),
            StartColumn = ParseUnsignedInt(legacy.StartCol) ?? 0,
            StartRow = ParseUnsignedInt(legacy.StartRow) ?? 0,
            EndColumn = ParseUnsignedInt(legacy.EndCol) < ParseUnsignedInt(legacy.StartRow) ? 
                ParseUnsignedInt(legacy.EndCol) + ParseUnsignedInt(legacy.StartRow) : 
                ParseUnsignedInt(legacy.EndCol)
        };
        
        return result;
    }
}