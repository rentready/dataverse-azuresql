using Azure.Data.Tables;
using Microsoft.Extensions.Azure;

namespace RR.DataverseAzureSql.Common.Extensions;

public static class AzureClientFactoryExtensions
{
    public static TableClient CreateClient<TOptions>(this IAzureClientFactory<TableClient> azureClientFactory)
         where TOptions : class, new() =>
            azureClientFactory.CreateClient(typeof(TOptions).ToString().Split('.')[^1]);
}

