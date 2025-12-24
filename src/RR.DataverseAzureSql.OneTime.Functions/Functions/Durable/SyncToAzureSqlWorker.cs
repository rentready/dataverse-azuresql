using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RR.Common.Validations;
using RR.DataverseAzureSql.Common.Dtos;
using RR.DataverseAzureSql.Common.Enums;
using RR.DataverseAzureSql.OneTime.Functions.Dtos;
using RR.DataverseAzureSql.Services.Options.Services.Sync;

namespace RR.DataverseAzureSql.OneTime.Functions.Functions.Durable;

public class SyncToAzureSqlWorker
{
    private readonly OneTimeSyncToAzureSqlServiceOptions _options;
    private readonly ILogger<SyncToAzureSqlWorker> _logger;

    public SyncToAzureSqlWorker(IOptions<OneTimeSyncToAzureSqlServiceOptions> options,
        ILogger<SyncToAzureSqlWorker> logger)
    {
        _logger = logger.IsNotNull(nameof(logger));
        _options = options.Value;
    }

    [Function(nameof(SyncToAzureSqlWorker))]
    public async Task Run(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var input = context.GetInput<WorkerInputDto>();

        var startTime = context.CurrentUtcDateTime;
        var taskOptions = new TaskOptions(new TaskRetryOptions(
            new RetryPolicy(_options.MaxActivityRetryCount, TimeSpan.FromSeconds(_options.ActivityRetryIntervalInSec))));

        var status = new EntitySyncStatusDto
        {
            Type = input.Type,
            EntityLogicalName = input.EntityLogicalName,
            Token = string.Empty,
            PageNumber = 1,
            PagingCookie = string.Empty,
            TotalNewOrUpdatedEntities = 0,
            TotalRemovedOrDeletedEntities = 0
        };

        if (input.Type == SyncType.Full)
        {
            await context.CallActivityAsync(nameof(CleanUpAzureSqlTableActivity),
                input.EntityLogicalName, taskOptions);
        }

        await context.CallActivityAsync(nameof(CreateOrUpdateAzureSqlTableSchemaActivity),
            input.EntityLogicalName, taskOptions);

        while (string.IsNullOrEmpty(status.Token))
        {
            status = await context.CallActivityAsync<EntitySyncStatusDto>(nameof(SyncToAzureSqlActivity),
                status, taskOptions);
        }

        _logger.LogInformation("{entityLogicalName} Start synchronization time: {startTime}" +
            "Type: {inputType}, " +
            "Total new or updated entities: {statusTotalNewOrUpdatedEntities}, " +
            "Total removed or deleted entities: {status.TotalRemovedOrDeletedEntities}, " +
            "Total execution time: {totalTime}",
            input.EntityLogicalName, startTime, input.Type, status.TotalNewOrUpdatedEntities,
            status.TotalRemovedOrDeletedEntities, context.CurrentUtcDateTime.Subtract(startTime));
    }
}

