namespace RR.DataverseAzureSql.Common.Interfaces.Services.AzureSql;

public interface IAzureSqlChangesSyncService
{
    Task BulkUpsertAsync(string entityLogicalName, List<Dictionary<string, object>> records, CancellationToken ct);
    Task BulkDeleteAsync(string entityLogicalName, List<Guid> ids, CancellationToken ct);
}

