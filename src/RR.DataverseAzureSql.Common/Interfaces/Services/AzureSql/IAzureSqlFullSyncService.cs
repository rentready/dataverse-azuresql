namespace RR.DataverseAzureSql.Common.Interfaces.Services.AzureSql;

public interface IAzureSqlFullSyncService
{
    Task BulkCopyAsync(string entityLogicalName, List<Dictionary<string, object>> records, CancellationToken ct);
    Task CleanUpTableAsync(string entityLogicalName, CancellationToken ct);
}

