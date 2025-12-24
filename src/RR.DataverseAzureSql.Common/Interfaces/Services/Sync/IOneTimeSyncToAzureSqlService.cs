using RR.DataverseAzureSql.Common.Dtos;

namespace RR.DataverseAzureSql.Common.Interfaces.Services.Sync;

public interface IOneTimeSyncToAzureSqlService
{
    Task<EntitySyncStatusDto> Sync(EntitySyncStatusDto status, CancellationToken ct);
}

