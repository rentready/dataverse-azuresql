using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using RR.Common.Validations;
using RR.DataverseAzureSql.Common.Dtos;
using RR.DataverseAzureSql.Common.Interfaces.Services.Sync;

namespace RR.DataverseAzureSql.OneTime.Functions.Functions.Durable;

public class SyncToAzureSqlActivity
{
    private readonly IOneTimeSyncToAzureSqlService _syncToAzureSqlService;
    private readonly ILogger<SyncToAzureSqlActivity> _logger;

    public SyncToAzureSqlActivity(IOneTimeSyncToAzureSqlService syncToAzureSqlService,
        ILogger<SyncToAzureSqlActivity> logger)
    {
        _syncToAzureSqlService = syncToAzureSqlService.IsNotNull(nameof(syncToAzureSqlService));
        _logger = logger.IsNotNull(nameof(logger));
    }

    [Function(nameof(SyncToAzureSqlActivity))]
    public async Task<EntitySyncStatusDto> Run(
        [ActivityTrigger] EntitySyncStatusDto status, FunctionContext executionContext)
    {
        _logger.LogInformation("{entityLogicalName} Status: Running, " +
            "Type: {statusType}, " +
            "Total new or updated entities: {statusTotalNewOrUpdatedEntities}, " +
            "Total removed or deleted entities: {statusTotalRemovedOrDeletedEntities}",
            status.EntityLogicalName, status.Type, status.TotalNewOrUpdatedEntities, status.TotalRemovedOrDeletedEntities);
        return await _syncToAzureSqlService.Sync(status, executionContext.CancellationToken);
    }
}

