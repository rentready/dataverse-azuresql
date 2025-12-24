using Azure.Messaging.ServiceBus;
using Microsoft.Xrm.Sdk;
using RR.DataverseAzureSql.Common.Dtos;

namespace RR.DataverseAzureSql.Common.Interfaces.Services.ServiceBus;

public interface IMessageManagerService
{
    Entity ProcessNewOrUpdatedMessage(ServiceBusReceivedMessage message);
    EntityReference ProcessRemovedOrDeletedMessage(ServiceBusReceivedMessage message);
    EntityNameAssociateRequestMappingDto ProcessAssociatedMessage(ServiceBusReceivedMessage message);
    EntityNameDisassociateRequestMappingDto ProcessDisassociatedMessage(ServiceBusReceivedMessage message);
    List<string> GetRelationshipEntityLogicalNames();
}

