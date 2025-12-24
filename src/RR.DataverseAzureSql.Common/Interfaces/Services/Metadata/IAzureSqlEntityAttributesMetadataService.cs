using Microsoft.Xrm.Sdk.Metadata;
using RR.DataverseAzureSql.Common.Dtos;

namespace RR.DataverseAzureSql.Common.Interfaces.Services.Metadata;

public interface IAzureSqlEntityAttributesMetadataService
{
    List<AzureSqlColumnDto> Get(IEnumerable<AttributeMetadata> attributes);
}

