using Azure;

namespace RR.DataverseAzureSql.Common.Interfaces.Services.Storage;

public interface ITableService
{
    string GetDeltalink(string partitionKey);
    Response SetDeltalink(string partitionKey, string deltalink);
}

