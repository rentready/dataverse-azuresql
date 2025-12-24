using Microsoft.Xrm.Sdk;

namespace RR.DataverseAzureSql.Common.Interfaces.Services.Converters;

public interface IEntityConverter
{
    Dictionary<string, object> ToDictionary(Entity entity);
}

