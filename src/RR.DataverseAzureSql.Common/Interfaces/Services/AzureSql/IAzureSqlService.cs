namespace RR.DataverseAzureSql.Common.Interfaces.Services.AzureSql;

public interface IAzureSqlService
{
    Task UpsertAsync(string entityLogicalName, Dictionary<string, object> record, CancellationToken ct);
    Task DeleteAsync(string entityLogicalName, Dictionary<string, object> record, CancellationToken ct);
    Task DeleteAsync(string entityLogicalName, Guid id, CancellationToken ct);
}

