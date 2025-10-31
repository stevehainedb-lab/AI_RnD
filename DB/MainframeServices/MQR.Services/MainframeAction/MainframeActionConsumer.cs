using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MQR.DataAccess.Context;
using MQR.DataAccess.Entities;
using MQR.Services.MainframeAction.Sessions;
using MQR.Services.Observability;
using System.Diagnostics;

namespace MQR.Services.MainframeAction;

public class MainframeActionConsumer(
    ISessionProvider sessionProvider,
    IInstructionSetProvider instructionSetProvider,
    MQRDbContext dbContext,
    ILogger<MainframeActionConsumer> logger) : IConsumer<MainframeActionNotification>
{
    public async Task Consume(ConsumeContext<MainframeActionNotification> context)
    {
        using var loggerScope = logger.BeginScope(context.Message.RequestId);
        using var activity = MqrTracing.ActivitySource.StartActivity("MainframeAction.Consume", ActivityKind.Consumer);
        activity?.SetTag("messaging.system", "masstransit");
        activity?.SetTag("mqr.request.id", context.Message.RequestId);
        activity?.SetTag("mqr.logon.instructionSet", context.Message.Request.LogonInstructionSet);
        activity?.SetTag("mqr.query.instructionSet", context.Message.Request.QueryInstructionSet);
        logger.LogInformation("Consuming mainframe action notification {RequestId}", context.Message.RequestId);

        // Set initial status to InProgress
        var requestSession = await dbContext.RequestSessions.FirstAsync(r => r.RequestId == context.Message.RequestId, context.CancellationToken);
        requestSession.Status = RequestStatus.InProgress;
        await dbContext.SaveChangesAsync(context.CancellationToken);
        
        var queryInstructionSet = await instructionSetProvider.GetQueryInstructionSet(context.Message.Request.QueryInstructionSet, context.CancellationToken);

        var session = await sessionProvider.AcquireSessionLockAsync(
            context.Message.Request.LogonInstructionSet,
            context.Message.RequestId, context.CancellationToken);
        logger.LogInformation("Got session {SessionId}", session.SessionId);

        try
        {
            // Update status to InvokingMainframeQuery before running the query
            requestSession.Status = RequestStatus.InvokingMainframeQuery;
            await dbContext.SaveChangesAsync(context.CancellationToken);

            logger.LogInformation("Running query on session {SessionId}", session.SessionId);
            var runResult = await session.RunQueryAsync(context.Message.Request, queryInstructionSet, context.CancellationToken);
            dbContext.TransactionData.AddRange(runResult.Transactions);
            await dbContext.SaveChangesAsync(context.CancellationToken);
            logger.LogInformation("Query completed on session {SessionId}", session.SessionId);
        }
        catch (OperationCanceledException ocex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "canceled");
            activity?.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
            {
                { "exception.type", ocex.GetType().FullName },
                { "exception.message", ocex.Message }
            }));
            logger.LogWarning("Operation canceled while processing {RequestId}", context.Message.RequestId);
            requestSession.Status = RequestStatus.Failed;
            await dbContext.SaveChangesAsync(CancellationToken.None);
            throw;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
            {
                { "exception.type", ex.GetType().FullName },
                { "exception.message", ex.Message }
            }));
            logger.LogError(ex, "Error while processing {RequestId}", context.Message.RequestId);
            requestSession.Status = RequestStatus.Failed;
            await dbContext.SaveChangesAsync(CancellationToken.None);
            throw;
        }
        finally
        {
            await sessionProvider.ReleaseSessionLockAsync(session.SessionId, context.CancellationToken);
            logger.LogInformation("Released session {SessionId}", session.SessionId);
        }
        
        activity?.SetStatus(ActivityStatusCode.Ok);
        logger.LogInformation("Mainframe action completed successfully for {RequestId}", context.Message.RequestId);
    }
}