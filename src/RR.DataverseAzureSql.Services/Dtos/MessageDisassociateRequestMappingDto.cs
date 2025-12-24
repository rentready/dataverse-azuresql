using Azure.Messaging.ServiceBus;
using RR.DataverseAzureSql.Common.Dtos;

namespace RR.DataverseAzureSql.Services.Dtos;

public class MessageDisassociateRequestMappingDto
{
    public ServiceBusReceivedMessage Message { get; set; }
    public EntityNameDisassociateRequestMappingDto EntityNameDisassociateRequestMapping { get; set; }
}

