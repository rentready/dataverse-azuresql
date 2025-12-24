using Microsoft.Xrm.Sdk.Messages;

namespace RR.DataverseAzureSql.Common.Dtos;

public class EntityNameAssociateRequestMappingDto
{
    public string EntityLogicalName { get; set; }
    public AssociateRequest Request { get; set; }
}

