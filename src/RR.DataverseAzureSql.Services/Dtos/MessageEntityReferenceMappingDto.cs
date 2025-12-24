using Azure.Messaging.ServiceBus;
using Microsoft.Xrm.Sdk;

namespace RR.DataverseAzureSql.Services.Dtos;

public class MessageEntityReferenceMappingDto
{
    public ServiceBusReceivedMessage Message { get; set; }
    public EntityReference EntityReference { get; set; }
}

