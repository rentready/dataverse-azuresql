using RR.DataverseAzureSql.Common.Enums;

namespace RR.DataverseAzureSql.Common.Dtos;

public class EntitySyncStatusDto
{
    public SyncType Type { get; set; }
    public string EntityLogicalName { get; set; }
    public string Token { get; set; }
    public string PagingCookie { get; set; }
    public int PageNumber { get; set; }
    public int TotalNewOrUpdatedEntities { get; set; }
    public int TotalRemovedOrDeletedEntities { get; set; }
}

