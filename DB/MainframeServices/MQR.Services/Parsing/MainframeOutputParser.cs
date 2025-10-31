using System.Text.Json;
using Microsoft.Extensions.Logging;
using MQR.DataAccess.Context;
using MQR.DataAccess.Entities;
using MQR.Services.Instructions;
using MQR.Services.Instructions.Models.Parsers;
using MQR.WebAPI.ServiceModel;

namespace MQR.Services.Queues;

/// <summary>
/// Parses raw mainframe output, according to a given parse instruction set.
/// </summary>
public class MainframeOutputParser(
    IInstructionSetProvider instructionSetProvider,
    ScreenCaptureParser screenCaptureParser,
    InstructionBasedParser instructionParser,
    RawParser rawParser,
    MainframeResponseValidator validator,
    ILogger<MainframeOutputParser> logger)
{
    public async Task<QueryResult> ParseAsync(ParseNotification notification, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Starting parse for request {RequestId} with session {SessionId}",
            notification.RequestId,
            notification.Result.SessionId);

        try
        {
            var instructions = await instructionSetProvider.GetParseInstructionSet(notification.ParseInstructionSetIdentifier, ct);
            await validator.ValidateMainframeResponse(notification, instructions);
            var result = await PerformParsing(notification, instructions);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to parse request {RequestId}: {Message}",
                notification.RequestId,
                ex.Message);
            throw;
        }
    }
    
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };
    
    public async Task PersistParsedOutput(ParseNotification context, QueryResult result, MQRDbContext db, CancellationToken ct = default)
    {
        var id = context.RequestId;

        var record = await db.RequestSessions.FindAsync([id], ct);

        if (record is null)
        {
            logger.LogError(
                "Request session not found for ID {RequestId}",
                id);

            throw new InvalidOperationException($"No request session found for ID {id}.");
        }

        var json = JsonSerializer.Serialize(result, SerializerOptions);

        // Mark this session as complete.
        // This means we can give data back to end-users now if they call the right endpoint.
        record.ParsedMainframeOutput = new ParsedOutput()
        {
            RequestId = record.RequestId,
            ParsedMainframeJson = json
        };

        record.Status = RequestStatus.Complete;

        try
        {
            await db.SaveChangesAsync(ct);

            logger.LogInformation(
                "Successfully saved parsed output for request {RequestId} ({OutputSize} bytes)",
                id,
                json.Length);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error saving parsed output for request {RequestId}", id);

            record.Status = RequestStatus.Failed;
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task<QueryResult> PerformParsing(ParseNotification notification, ParseInstructionSet instructions)
    {
        var allSections = new List<QueryResultSection>();

        // Process screen capture data and get cleaned raw data
        var raw = notification.Result.RawMainframeResponse;
        var (parseableData, screenCaptureSection) = screenCaptureParser.ProcessScreenCaptureData(raw);

        if (screenCaptureSection != null)
        {
            allSections.Add(screenCaptureSection);
        }

        // Parse based on instruction set configuration
        if (instructions.CaptureRaw)
        {
            var rawSection = rawParser.ParseRawData(parseableData);
            allSections.Add(rawSection);
        }
        else
        {
            var parsedSections = await instructionParser.ParseByInstructionSet(notification, instructions, parseableData);
            allSections.AddRange(parsedSections);
        }

        var result = new QueryResult
        {
            MFReponseTime = notification.MFResponseTime,
            QueryResultSections = allSections.ToArray()
        };

        logger.LogInformation(
            "Successfully parsed request {RequestId} with {SectionCount} sections",
            notification.RequestId,
            result.QueryResultSections.Length);

        return result;
    }
}