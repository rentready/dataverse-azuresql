namespace RR.DataverseAzureSql.Common.Interfaces.Services.AzureSql
{
    public interface ISqlSchemaService
    {
        public Task CreateOrUpdateTableSchema(string tableName, CancellationToken ct);
    }
}
