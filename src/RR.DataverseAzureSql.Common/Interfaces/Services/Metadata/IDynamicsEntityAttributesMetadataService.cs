using Microsoft.Xrm.Sdk.Metadata;

namespace RR.DataverseAzureSql.Common.Interfaces.Services.Metadata;

public interface IDynamicsEntityAttributesMetadataService
{
    public Task<EntityMetadata> GetAsync(string entityLogicalName);
}

