using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using Microsoft.Xrm.Sdk;
using RR.DataverseAzureSql.Common.Dtos;
using RR.DataverseAzureSql.Common.Extensions;
using RR.DataverseAzureSql.Common.Interfaces.Services.ServiceBus;
using RR.DataverseAzureSql.Services.Options.Services.Sync;

namespace RR.DataverseAzureSql.Services.Services.ServiceBus;

public class MessageManagerService : IMessageManagerService
{
    private readonly RealTimeSyncToAzureSqlServiceOptions _options;

    public MessageManagerService(IOptions<RealTimeSyncToAzureSqlServiceOptions> options)
    {
        _options = options.Value;
    }

    public Entity ProcessNewOrUpdatedMessage(ServiceBusReceivedMessage message)
    {
        var context = GetRemoteExecutionContext(message);

        return context?.InputParameters?.ToEntity();
    }

    public EntityReference ProcessRemovedOrDeletedMessage(ServiceBusReceivedMessage message)
    {
        var context = GetRemoteExecutionContext(message);

        return context?.InputParameters?.ToEntityReference();
    }

    public EntityNameAssociateRequestMappingDto ProcessAssociatedMessage(ServiceBusReceivedMessage message)
    {
        var entityLogicalNames = GetRelationshipEntityLogicalNames();
        if (entityLogicalNames.Any())
        {
            var context = GetRemoteExecutionContext(message);
            var changedEntityLogicalName = context.SharedVariables.GetChangedEntityLogicalName();
            if (entityLogicalNames.Contains(changedEntityLogicalName))
            {
                return new EntityNameAssociateRequestMappingDto
                {
                    EntityLogicalName = changedEntityLogicalName,
                    Request = context.InputParameters.ToAssociateRequest()
                };
            }
        }

        return null;
    }

    public EntityNameDisassociateRequestMappingDto ProcessDisassociatedMessage(ServiceBusReceivedMessage message)
    {
        var entityLogicalNames = GetRelationshipEntityLogicalNames();
        if (entityLogicalNames.Any())
        {
            var context = GetRemoteExecutionContext(message);
            var changedEntityLogicalName = context.SharedVariables.GetChangedEntityLogicalName();
            if (entityLogicalNames.Contains(changedEntityLogicalName))
            {
                return new EntityNameDisassociateRequestMappingDto
                {
                    EntityLogicalName = changedEntityLogicalName,
                    Request = context.InputParameters.ToDisassociateRequest()
                };
            }
        }

        return null;
    }

    public List<string> GetRelationshipEntityLogicalNames()
    {
        var names = _options.RelationshipEntityLogicalNames;

        return string.IsNullOrEmpty(names) ? new List<string>() : names.Split(",").ToList();
    }

    private static RemoteExecutionContext GetRemoteExecutionContext(ServiceBusReceivedMessage message)
    {
        if (message.Body is not null)
        {
            var context = message.ToRemoteExecutionContext();

            if (context is not null && context.InputParameters.Count > 0)
            {
                return context;
            }

            return null;
        }

        return null;
    }
}

