using Microsoft.Xrm.Sdk.Messages;

namespace RR.DataverseAzureSql.Common.Dtos;

public class EntityNameDisassociateRequestMappingDto
{
    public string EntityLogicalName { get; set; }
    public DisassociateRequest Request { get; set; }
}

