using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Azure;
using RR.Common.General;
using RR.Common.Validations;
using RR.DataverseAzureSql.Common.Dtos;
using RR.DataverseAzureSql.Common.Extensions;
using RR.DataverseAzureSql.Common.Interfaces.Services.Storage;
using RR.DataverseAzureSql.Services.Options.Services.Storage;

namespace RR.DataverseAzureSql.Services.Services.Storage;

public class TableService : ITableService
{
    private readonly IAzureClientFactory<TableClient> _azureClientFactory;
    private readonly LazyWithoutExceptionCaching<TableClient> _lazyClient;

    protected TableClient TableClient => _lazyClient.Value;

    public TableService(IAzureClientFactory<TableClient> azureClientFactory)
    {
        _azureClientFactory = azureClientFactory.IsNotNull(nameof(azureClientFactory));
        _lazyClient = new LazyWithoutExceptionCaching<TableClient>(CreateTableClient);
    }

    public string GetDeltalink(string partitionKey)
    {
        var entities = TableClient
            .Query<DeltalinkDto>(filter: $"PartitionKey eq '{partitionKey}'");

        var deltalink = entities.Any()
            ? entities.First().Deltalink
            : string.Empty;

        return deltalink;
    }

    public Response SetDeltalink(string partitionKey, string deltalink)
    {
        var entities = TableClient
            .Query<DeltalinkDto>(filter: $"PartitionKey eq '{partitionKey}'");

        var response = entities.Any()
            ? UpsertDeltalink(entities.First(), deltalink)
            : InsertDeltalink(partitionKey, deltalink);

        return response;
    }

    private Response InsertDeltalink(string partitionKey, string deltalink)
    {
        var entity = new DeltalinkDto
        {
            PartitionKey = partitionKey,
            RowKey = Guid.NewGuid().ToString(),
            Deltalink = deltalink
        };

        return TableClient.AddEntity(entity);
    }

    private Response UpsertDeltalink(DeltalinkDto entity, string deltalink)
    {
        entity.Deltalink = deltalink;

        return TableClient.UpsertEntity(entity);
    }

    private TableClient CreateTableClient()
    {
        return _azureClientFactory.CreateClient<TableServiceOptions>();
    }
}

