using MQR.Services.Instructions.Models.Queries;
using MQR.Services.Instructions.Models.Shared;
using static MQR.Services.Instructions.Legacy.ParsingUtilities;

namespace MQR.Services.Instructions.Legacy;

/// <summary>
/// Maps legacy query instruction sets to the modern model.
/// </summary>
public static class QueryInstructionSetMapper
{
    public static QueryInstructionSet MapToNew(Query.QueryInstructionSet legacy)
    {
        // We deliberately do nothing with the legacy.CompatableParseList property.
        // It is not used in the modern model.

        var coreAction = new Models.Shared.ProcessAction()
        {
            Identifier = legacy.Identifier +"Action",
            NavigationAction = MapNavigationAction(legacy.NavigationAction, legacy.NavigationKey, legacy.NavigationTimeout),
            ScreenInputs = legacy.ScreenInput.Select(MapScreenInput).ToList(),
            SuccessCondition = legacy.SuccessCondition != null
                ? MapSuccessCondition(legacy.SuccessCondition)
                : null,
            ProcessActions = legacy.ProcessAction.Select(MapProcessAction).ToList()
        };
        
        return new QueryInstructionSet
        {
            Identifier = legacy.Identifier,
            ProcessActions = [coreAction]
        };
    }

    private static ScreenInput MapScreenInput(Query.ScreenInput legacy)
    {
        return new ScreenInput
        {
            
            Identifier = legacy.Identifier,
            Value = NullIfEmpty(legacy.Value),
            Position = new()
            {
                FieldNumber = ParseUnsignedInt(legacy.FieldNumber),
                StartColumn = ParseUnsignedInt(legacy.XPos),
                StartRow = ParseUnsignedInt(legacy.YPos)
            }
        };
    }

    private static SuccessCondition MapSuccessCondition(Query.SuccessCondition legacy)
    {
        return new SuccessCondition
        {
            LockCredentialOnFailure = ParseBool(legacy.LockCredentialOnFailure),
            ResetSessionOnFailure = ParseBool(legacy.ResetSessionOnFailure),
            ScreenIdentificationMarks = legacy.ScreenIdentificationMark
                .Select(MapScreenIdentificationMark)
                .ToList()
        };
    }
    
    private static ScreenIdentificationMark MapScreenIdentificationMark(
        Query.ScreenIdentificationMark legacy)
    {
        return new ScreenIdentificationMark
        {
            Identifier = NullIfEmpty(legacy.Identifier),
            WaitPeriod = ParseTimeSpan(legacy.WaitPeriod),
            RegExPattern = MapRegExPattern(legacy.RegExPattern),
            ScreenArea = MapScreenArea(legacy.ScreenArea)
        };
    }

    private static ScreenArea MapScreenArea(Query.ScreenArea legacy)
    {
        return new ScreenArea
        {
            StartColumn = ParseUnsignedInt(legacy.StartCol) ?? 0,
            StartRow = ParseUnsignedInt(legacy.StartRow) ?? 0,
            EndColumn = ParseUnsignedInt(legacy.EndCol),
            EndRow = ParseUnsignedInt(legacy.EndRow),
        };
    }

    private static RegExPattern MapRegExPattern(Query.RegExPattern legacy)
    {
        return new RegExPattern
        {
            RegExOptions = ParseRegexOptions(legacy.RegexOptions),
            Pattern = legacy.Value ?? string.Empty
        };
    }

    private static ProcessAction MapProcessAction(Query.ProcessAction legacy)
    {
        return new ProcessAction
        {
            Identifier = legacy.Identifier,
            IsCoreAction = ParseBool(legacy.IsCoreAction),
            NoPrintDataExpected = ParseBool(legacy.NoData),
            NavigationAction = MapNavigationAction(legacy.NavigationAction, legacy.NavigationKey, legacy.NavigationTimeout),
            ProcessActions = legacy.ProcessActions.Select(MapProcessAction).ToList(),
            ScreenCaptureDataPoints = legacy.ScreenCaptureDataPoint.Select(MapScreenCaptureDataPoint).ToList(),
            ScreenIdentificationMarks = legacy.ScreenIdentificationMark.Select(MapScreenIdentificationMark).ToList(),
            ScreenInputs = legacy.ScreenInput.Select(MapScreenInput).ToList()
        };
    }

    private static ScreenCaptureDataPoint MapScreenCaptureDataPoint(Query.ScreenCaptureDataPoint legacy)
    {
        return new ScreenCaptureDataPoint
        {
            ExceptIfNotFound = ParseBool(legacy.ExceptIfNotFound),
            Identifier = legacy.Identifier,
            RegExPattern = MapRegExPattern(legacy.RegExPattern),
            ScreenArea = MapScreenArea(legacy.ScreenArea)
        };
    }
}
