using Microsoft.Extensions.Logging;
using MQR.Services.Instructions;
using MQR.Services.Instructions.Models.Parsers;
using MQR.Services.MainframeAction.Sessions;

namespace MQR.Services.Queues;

public class MainframeResponseValidator(ISessionProvider sessionProvider, ILogger<MainframeResponseValidator> logger)
{
    public async Task ValidateMainframeResponse(ParseNotification notification, ParseInstructionSet instructions)
    {
        if (string.IsNullOrEmpty(notification.Result.RawMainframeResponse))
        {
            throw new InvalidOperationException(
                $"Parse instruction set: {instructions.Identifier} returned no data! " +
                $"RawData length was {notification.Result.RawMainframeResponse?.Length ?? 0}");
        }

        // Check for error conditions
        await ValidateNoErrorCondition(instructions, notification.Result.RawMainframeResponse);

        // Check for end of data markers
        ValidateEndOfDataPresent(instructions, notification.Result.RawMainframeResponse);
    }

    /// <summary>
    /// Validates that no error conditions are met in the raw data.
    /// </summary>
    private async Task ValidateNoErrorCondition(ParseInstructionSet instructionSet, string rawData)
    {
        var conditions = instructionSet.ErrorIdentifications
            .Where(e => e.IsMatch(rawData));

        foreach (var errorCondition in conditions)
        {
            logger.LogError(
                "Error identification mark {ErrorId} found in data for instruction set {InstructionSet}",
                errorCondition.Identifier,
                instructionSet.Identifier);

            if (errorCondition is { MakePoolUnavailable: true, PoolUnavailablePeriod: not null })
            {
                logger.LogWarning(
                    "Error condition {ErrorId} requires making pool unavailable for {Period} seconds",
                    errorCondition.Identifier,
                    errorCondition.PoolUnavailablePeriod.Value);

            }

            throw new InvalidOperationException(
                $"{errorCondition.Identifier} has been found in data. " +
                $"Parse instruction set: {instructionSet.Identifier}");
        }
    }

    /// <summary>
    /// Validates that required end-of-data markers are present.
    /// </summary>
    private void ValidateEndOfDataPresent(ParseInstructionSet instructionSet, string rawData)
    {
        var endOfData = instructionSet.EndOfDataIdentification;

        if (endOfData is { ExceptIfNotFound: true } && !endOfData.IsMatch(rawData))
        {
            throw new InvalidOperationException(
                $"{endOfData.Identifier} has not been found in data. " +
                $"Parse instruction set: {instructionSet.Identifier}");
        }
    }
}