using Newtonsoft.Json;
using RR.Common.Requests;
using RR.Common.Validations.Attributes;
using RR.DataverseAzureSql.Common.Enums;

namespace RR.DataverseAzureSql.OneTime.Functions.Models.Requests;

public class SyncEntitiesRequest : RequestBase
{
    [EnumValid]
    [JsonProperty("type")]
    public SyncType Type { get; set; }

    [JsonProperty("entities")]
    public List<string> Entities { get; set; }
}

