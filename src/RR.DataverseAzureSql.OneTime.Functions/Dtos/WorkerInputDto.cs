using RR.DataverseAzureSql.Common.Enums;

namespace RR.DataverseAzureSql.OneTime.Functions.Dtos;

public class WorkerInputDto
{
    public SyncType Type { get; set; }
    public string EntityLogicalName { get; set; }
}

