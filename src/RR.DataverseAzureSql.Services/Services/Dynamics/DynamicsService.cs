using Microsoft.Extensions.Options;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using RR.Common.Validations;
using RR.DataverseAzureSql.Common.Dtos;
using RR.DataverseAzureSql.Common.Enums;
using RR.DataverseAzureSql.Common.Interfaces.Services.Configs;
using RR.DataverseAzureSql.Common.Interfaces.Services.Dynamics;
using RR.DataverseAzureSql.Common.Interfaces.Services.Metadata;
using RR.DataverseAzureSql.Common.Interfaces.Services.Storage;
using RR.DataverseAzureSql.Services.Options.Services.Sync;

namespace RR.DataverseAzureSql.Services.Services.Dynamics;

public class DynamicsService : IDynamicsService
{
    private readonly IOrganizationService _organizationService;
    private readonly OneTimeSyncToAzureSqlServiceOptions _options;
    private readonly ITableService _tableService;
    private readonly IDynamicsEntityAttributesMetadataService _entityAttributesMetadataService;
    private readonly IRetrieveEntitySettingsConfigProvider _retrieveEntitySettingsConfigProvider;

    public DynamicsService(IOrganizationService organizationService,
        ITableService tableService,
        IOptions<OneTimeSyncToAzureSqlServiceOptions> options,
        IDynamicsEntityAttributesMetadataService entityAttributesMetadataService,
        IRetrieveEntitySettingsConfigProvider retrieveEntitySettingsConfigProvider)
    {
        _organizationService = organizationService.IsNotNull(nameof(organizationService));
        _tableService = tableService.IsNotNull(nameof(tableService));
        _options = options.Value;
        _entityAttributesMetadataService = entityAttributesMetadataService
            .IsNotNull(nameof(entityAttributesMetadataService));
        _retrieveEntitySettingsConfigProvider = retrieveEntitySettingsConfigProvider
            .IsNotNull(nameof(retrieveEntitySettingsConfigProvider));
    }

    public EntityCollection RetrieveAssociateEntityResponse(string entityLogicalName, AssociateRequest request)
    {
        var targetName = $"{request.Target.LogicalName}id";
        var relatedName = $"{request.RelatedEntities[0].LogicalName}id";

        var query = new QueryExpression()
        {
            Distinct = false,
            EntityName = entityLogicalName,
            ColumnSet = new ColumnSet(true),
            Criteria =
            {
                Filters =
                {
                    new FilterExpression
                    {
                        FilterOperator = LogicalOperator.And,
                        Conditions =
                        {
                            new ConditionExpression(targetName, ConditionOperator.In, request.Target.Id),
                            new ConditionExpression(relatedName, ConditionOperator.In, request.RelatedEntities.Select(x => x.Id).ToList())
                        }
                    }
                }
            }
        };

        return _organizationService.RetrieveMultiple(query);
    }

    public async Task<RetrieveEntityChangesResponse> RetrieveEntityChangesResponse(EntitySyncStatusDto status)
    {
        var request = await GetRetrieveEntityChangesRequest(status);
        return (RetrieveEntityChangesResponse)_organizationService.Execute(request);
    }

    private async Task<RetrieveEntityChangesRequest> GetRetrieveEntityChangesRequest(EntitySyncStatusDto status)
    {
        string token;

        if (string.IsNullOrEmpty(status.Token))
        {
            token = status.Type == SyncType.Full
                ? ""
                : _tableService.GetDeltalink(status.EntityLogicalName);
        }
        else
        {
            token = status.Token;
        }

        return new RetrieveEntityChangesRequest()
        {
            EntityName = status.EntityLogicalName,
            Columns = await GetColumnSet(status.EntityLogicalName),
            DataVersion = token,
            PageInfo = new PagingInfo()
            {
                Count = _options.BatchSize,
                PageNumber = status.PageNumber,
                PagingCookie = status.PagingCookie,
                ReturnTotalRecordCount = false
            }
        };
    }

    internal async Task<ColumnSet> GetColumnSet(string entityLogicalName)
    {
        var metadata = await _entityAttributesMetadataService.GetAsync(entityLogicalName);
        var retrieveEntitySettings = _retrieveEntitySettingsConfigProvider.Get();
        var settings = Array.Find(retrieveEntitySettings, x => x.EntityLogicalName == entityLogicalName);
        var columnsQuery = metadata.Attributes.Where(x => x.AttributeOf == null
            && x.IsValidForRead != false
            && x.Description?.LocalizedLabels?.All(x => x.Label == null || !x.Label.Contains("internal use only")) != false).Select(x => x.LogicalName);
        if (settings != null && settings.ExcludedFields.Any())
        {
            columnsQuery = columnsQuery.Where(x => !settings.ExcludedFields.Contains(x));
        }
        return new ColumnSet(columnsQuery.ToArray());
    }
}

