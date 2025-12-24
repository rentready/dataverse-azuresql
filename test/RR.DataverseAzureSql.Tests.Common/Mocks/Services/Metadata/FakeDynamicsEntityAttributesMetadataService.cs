using System;
using Microsoft.Xrm.Sdk.Metadata;
using RR.DataverseAzureSql.Common.Interfaces.Services.Metadata;
using RR.DataverseAzureSql.Services.Services.Metadata;

namespace RR.DataverseAzureSql.Tests.Common.Mocks.Services.Metadata;

public class FakeDynamicsEntityAttributesMetadataService : IDynamicsEntityAttributesMetadataService
{
    public Task<EntityMetadata> GetAsync(string entityLogicalName)
    {
        throw new NotImplementedException();
    }
}

