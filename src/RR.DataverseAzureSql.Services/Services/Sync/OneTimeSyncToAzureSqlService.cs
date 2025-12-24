using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using RR.Common.Validations;
using RR.DataverseAzureSql.Common.Dtos;
using RR.DataverseAzureSql.Common.Enums;
using RR.DataverseAzureSql.Common.Interfaces.Services.AzureSql;
using RR.DataverseAzureSql.Common.Interfaces.Services.Converters;
using RR.DataverseAzureSql.Common.Interfaces.Services.Dynamics;
using RR.DataverseAzureSql.Common.Interfaces.Services.Storage;
using RR.DataverseAzureSql.Common.Interfaces.Services.Sync;

namespace RR.DataverseAzureSql.Services.Services.Sync;

public class OneTimeSyncToAzureSqlService : IOneTimeSyncToAzureSqlService
{
    private readonly IDynamicsService _dynamicsService;
    private readonly IAzureSqlFullSyncService _azureSqlFullSyncService;
    private readonly IAzureSqlChangesSyncService _azureSqlChangesSyncService;
    private readonly ITableService _tableService;
    private readonly IEntityConverter _entityConverter;
    private readonly ILogger<OneTimeSyncToAzureSqlService> _logger;

    public OneTimeSyncToAzureSqlService(IDynamicsService dynamicsService,
        IAzureSqlFullSyncService azureSqlFullSyncService,
        IAzureSqlChangesSyncService azureSqlChangesSyncService,
        ITableService tableService,
        IEntityConverter entityConverter,
        ILogger<OneTimeSyncToAzureSqlService> logger)
    {
        _dynamicsService = dynamicsService.IsNotNull(nameof(dynamicsService));
        _azureSqlFullSyncService = azureSqlFullSyncService.IsNotNull(nameof(azureSqlFullSyncService));
        _azureSqlChangesSyncService = azureSqlChangesSyncService.IsNotNull(nameof(azureSqlChangesSyncService));
        _tableService = tableService.IsNotNull(nameof(tableService));
        _entityConverter = entityConverter.IsNotNull(nameof(entityConverter));
        _logger = logger.IsNotNull(nameof(logger));
    }

    public async Task<EntitySyncStatusDto> Sync(EntitySyncStatusDto status, CancellationToken ct)
    {
        var newOrUpdatedEntities = new List<Entity>();
        var removedOrDeletedEntities = new List<EntityReference>();

        try
        {
            _logger.LogInformation("[{entityLogicalName}]: Going to retrieve entity changes.", status.EntityLogicalName);
            var response = await _dynamicsService.RetrieveEntityChangesResponse(status);

            newOrUpdatedEntities.AddRange(response.EntityChanges.Changes
                .Where(x => x is NewOrUpdatedItem)
                .Select(x => (x as NewOrUpdatedItem).NewOrUpdatedEntity)
                .ToArray());

            removedOrDeletedEntities.AddRange(response.EntityChanges.Changes
                .Where(x => x is RemovedOrDeletedItem)
                .Select(x => (x as RemovedOrDeletedItem).RemovedItem)
                .ToArray());

            if (newOrUpdatedEntities.Any())
            {
                _logger.LogInformation("[{entityLogicalName}]: Going to upsert entities.", status.EntityLogicalName);
                await BulkUpsertEntities(status.EntityLogicalName, status.Type, newOrUpdatedEntities, ct);
            }

            if (removedOrDeletedEntities.Any())
            {
                _logger.LogInformation("[{entityLogicalName}]: Going to delete entities.", status.EntityLogicalName);
                await BulkDeleteEntities(status.EntityLogicalName, removedOrDeletedEntities, ct);
            }

            var updatedStatus = UpdateSyncStatus(response, status);
            updatedStatus.TotalNewOrUpdatedEntities += newOrUpdatedEntities.Count;
            updatedStatus.TotalRemovedOrDeletedEntities += removedOrDeletedEntities.Count;

            return updatedStatus;
        }
        catch (Exception ex)
        {
            _logger.LogError("{ex}", ex);
            throw;
        }
    }

    private async Task BulkUpsertEntities(string entityLogicalName, SyncType syncType, List<Entity> entities, CancellationToken ct)
    {
        var records = entities.Select(entity => _entityConverter.ToDictionary(entity)).ToList();
        try
        {
            if (syncType == SyncType.Full)
            {
                await _azureSqlFullSyncService.BulkCopyAsync(entityLogicalName, records, ct);
            }
            else
            {
                await _azureSqlChangesSyncService.BulkUpsertAsync(entityLogicalName, records, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("{ex}", ex);
            throw;
        }
    }

    private async Task BulkDeleteEntities(string entityLogicalName, List<EntityReference> entities, CancellationToken ct)
    {
        var ids = entities.Select(entity => entity.Id).ToList();
        try
        {
            await _azureSqlChangesSyncService.BulkDeleteAsync(entityLogicalName, ids, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ex}", ex);
            throw;
        }
    }

    private EntitySyncStatusDto UpdateSyncStatus(RetrieveEntityChangesResponse response, EntitySyncStatusDto status)
    {
        if (response.EntityChanges.MoreRecords)
        {
            status.PageNumber = ++status.PageNumber;
            status.PagingCookie = response.EntityChanges.PagingCookie;
        }
        else
        {
            status.Token = response.EntityChanges.DataToken;
            _tableService.SetDeltalink(status.EntityLogicalName, status.Token);
        }

        return status;
    }
}

