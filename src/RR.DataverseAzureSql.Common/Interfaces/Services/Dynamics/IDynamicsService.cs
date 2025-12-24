using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using RR.DataverseAzureSql.Common.Dtos;

namespace RR.DataverseAzureSql.Common.Interfaces.Services.Dynamics;

public interface IDynamicsService
{
    EntityCollection RetrieveAssociateEntityResponse(string entityLogicalName, AssociateRequest request);
    Task<RetrieveEntityChangesResponse> RetrieveEntityChangesResponse(EntitySyncStatusDto status);
}

