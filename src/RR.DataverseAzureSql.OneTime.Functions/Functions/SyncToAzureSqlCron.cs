using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RR.Common.Validations;
using RR.DataverseAzureSql.Common.Enums;
using RR.DataverseAzureSql.Common.Interfaces.Services.Telemetry;
using RR.DataverseAzureSql.OneTime.Functions.Functions.Durable;
using RR.DataverseAzureSql.OneTime.Functions.Models.Requests;
using RR.DataverseAzureSql.Services.Options.Services.Sync;

namespace RR.DataverseAzureSql.OneTime.Functions.Functions;

public class SyncToAzureSqlCron
{
    private readonly ICustomMetricsService _customMetricsService;
    private readonly OneTimeSyncToAzureSqlServiceOptions _options;
    private readonly ILogger<SyncToAzureSqlCron> _logger;

    public SyncToAzureSqlCron(ICustomMetricsService customMetricsService,
        IOptions<OneTimeSyncToAzureSqlServiceOptions> options,
        ILogger<SyncToAzureSqlCron> logger)
    {
        _customMetricsService = customMetricsService.IsNotNull(nameof(customMetricsService));
        _logger = logger.IsNotNull(nameof(logger));
        _options = options.Value;
    }

    [Function(nameof(SyncToAzureSqlCron))]
    public async Task Run(
       [TimerTrigger("%TriggerTime%", RunOnStartup = false)] TimerInfo timerInfo,
       [DurableClient] DurableTaskClient durableClient, FunctionContext executionContext)
    {
        var request = new SyncEntitiesRequest
        {
            Type = SyncType.Changes,
            Entities = _options.SynchronizedEntityLogicalNames
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .ToList()
        };

        var result = durableClient.GetAllInstancesAsync();
        if (result is not null)
        {
            await foreach (var item in result)
            {
                if (item.InstanceId.Equals(SyncType.Full.ToString(), StringComparison.OrdinalIgnoreCase)
                    || item.InstanceId.Equals(SyncType.Changes.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    var existingState = await durableClient.GetInstancesAsync(item.InstanceId, getInputsAndOutputs: true, executionContext.CancellationToken);

                    TrackMetricIfNeeded(Enum.Parse<SyncType>(item.InstanceId, true), existingState);

                    if (existingState.RuntimeStatus == OrchestrationRuntimeStatus.Running
                        || existingState.RuntimeStatus == OrchestrationRuntimeStatus.Pending
                        || existingState.RuntimeStatus == OrchestrationRuntimeStatus.Suspended)
                    {
                        _logger.LogWarning("An instance with ID {instanceId} already exists. Details: {state}", item.InstanceId, existingState.RuntimeStatus);
                        return;
                    }
                }
            }
        }
        await durableClient.ScheduleNewOrchestrationInstanceAsync(nameof(SyncToAzureSqlOrchestrator),
            request, new StartOrchestrationOptions { InstanceId = request.Type.ToString() }, executionContext.CancellationToken);
    }

    private void TrackMetricIfNeeded(SyncType syncType, OrchestrationMetadata metadata)
    {
        if (syncType == SyncType.Changes)
        {
            _customMetricsService.TrackOrchestrationRuntimeStatus(syncType, (int)metadata.RuntimeStatus);
        }
    }
}

