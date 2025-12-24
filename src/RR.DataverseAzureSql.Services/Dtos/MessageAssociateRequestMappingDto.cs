using Azure.Messaging.ServiceBus;
using RR.DataverseAzureSql.Common.Dtos;

namespace RR.DataverseAzureSql.Services.Dtos;

public class MessageAssociateRequestMappingDto
{
    public ServiceBusReceivedMessage Message { get; set; }
    public EntityNameAssociateRequestMappingDto EntityNameAssociateRequestMapping { get; set; }
}

