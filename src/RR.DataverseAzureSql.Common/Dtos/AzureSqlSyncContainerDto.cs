namespace RR.DataverseAzureSql.Common.Dtos;

public class AzureSqlSyncContainerDto
{
    public List<Dictionary<string, object>> InsertedRecords { get; set; }
        = new List<Dictionary<string, object>>();
    public List<Dictionary<string, object>> UpdatedRecords { get; set; }
        = new List<Dictionary<string, object>>();
}

