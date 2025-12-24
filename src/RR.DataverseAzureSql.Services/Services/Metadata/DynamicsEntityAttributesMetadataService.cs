using System.Collections.Concurrent;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using RR.Common.Exceptions;
using RR.Common.Validations;
using RR.DataverseAzureSql.Common.Interfaces.Services.Metadata;

namespace RR.DataverseAzureSql.Services.Services.Metadata;

public class DynamicsEntityAttributesMetadataService : IDynamicsEntityAttributesMetadataService
{
    private readonly ConcurrentDictionary<string, Task<EntityMetadata>> _cache = new ConcurrentDictionary<string, Task<EntityMetadata>>();
    private readonly IOrganizationServiceAsync _organizationServiceAsync;

    public DynamicsEntityAttributesMetadataService(IOrganizationServiceAsync organizationServiceAsync)
    {
        _organizationServiceAsync = organizationServiceAsync.IsNotNull(nameof(organizationServiceAsync));
    }

    public async Task<EntityMetadata> GetAsync(string entityLogicalName)
    {
        var result = await _cache.GetOrAdd(entityLogicalName, ReceiveEntityMetadataFromDynamics);
        if (result == null)
        {
            throw new InconsistentDataException($"meatadata for entity {entityLogicalName} is not found");
        }
        return result;
    }

    private async Task<EntityMetadata> ReceiveEntityMetadataFromDynamics(string entityLogicalName)
    {
        var request = new RetrieveEntityRequest
        {
            EntityFilters = EntityFilters.Attributes,
            LogicalName = entityLogicalName
        };
        var response = (RetrieveEntityResponse)await _organizationServiceAsync.ExecuteAsync(request);
        return response.EntityMetadata;
    }
}

