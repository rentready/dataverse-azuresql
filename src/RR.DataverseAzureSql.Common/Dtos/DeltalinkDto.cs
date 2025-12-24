using Azure;
using Azure.Data.Tables;

namespace RR.DataverseAzureSql.Common.Dtos;

public class DeltalinkDto : ITableEntity
{
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
    public string Deltalink { get; set; }
}

