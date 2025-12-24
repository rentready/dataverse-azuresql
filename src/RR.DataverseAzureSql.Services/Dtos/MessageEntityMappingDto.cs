using Azure.Messaging.ServiceBus;
using Microsoft.Xrm.Sdk;

namespace RR.DataverseAzureSql.Services.Dtos;

internal class MessageEntityMappingDto
{
    public ServiceBusReceivedMessage Message { get; set; }
    public Entity Entity { get; set; }
}

