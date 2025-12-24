using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RR.DataverseAzureSql.Common.Enums;

[JsonConverter(typeof(StringEnumConverter))]
public enum SyncType
{
    Full = 1,
    Changes
}
