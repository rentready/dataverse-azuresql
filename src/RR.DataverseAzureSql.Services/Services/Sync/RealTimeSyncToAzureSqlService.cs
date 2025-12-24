using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RR.Common.Validations;
using RR.DataverseAzureSql.Common.Constants;
using RR.DataverseAzureSql.Common.Enums;
using RR.DataverseAzureSql.Common.Exceptions;
using RR.DataverseAzureSql.Common.Extensions;
using RR.DataverseAzureSql.Common.Interfaces.Services.AzureSql;
using RR.DataverseAzureSql.Common.Interfaces.Services.Converters;
using RR.DataverseAzureSql.Common.Interfaces.Services.Dynamics;
using RR.DataverseAzureSql.Common.Interfaces.Services.ServiceBus;
using RR.DataverseAzureSql.Common.Interfaces.Services.Sync;
using RR.DataverseAzureSql.Common.Interfaces.Services.Telemetry;
using RR.DataverseAzureSql.Services.Dtos;
using RR.DataverseAzureSql.Services.Options.Services.Sync;

namespace RR.DataverseAzureSql.Services.Services.Sync;

public class RealTimeSyncToAzureSqlService : IRealTimeSyncToAzureSqlService
{
    private readonly IDynamicsService _dynamicsService;
    private readonly IAzureSqlService _azureSqlService;
    private readonly IServiceBusService _serviceBusService;
    private readonly IMessageManagerService _messageManagerService;
    private readonly IEntityConverter _entityConverter;
    private readonly RealTimeSyncToAzureSqlServiceOptions _options;
    private readonly ILogger<RealTimeSyncToAzureSqlService> _logger;
    private readonly ISqlSchemaService _sqlSchemaService;
    private readonly ICustomMetricsService _customMetricsService;

    public RealTimeSyncToAzureSqlService(IDynamicsService dynamicsService,
        IAzureSqlService azureSqlService,
        IServiceBusService serviceBusService,
        IMessageManagerService messageManagerService,
        IEntityConverter entityConverter,
        IOptions<RealTimeSyncToAzureSqlServiceOptions> options,
        ISqlSchemaService sqlSchemaService,
        ICustomMetricsService customMetricsService,
        ILogger<RealTimeSyncToAzureSqlService> logger)
    {
        _dynamicsService = dynamicsService.IsNotNull(nameof(dynamicsService));
        _azureSqlService = azureSqlService.IsNotNull(nameof(azureSqlService));
        _serviceBusService = serviceBusService.IsNotNull(nameof(serviceBusService));
        _messageManagerService = messageManagerService.IsNotNull(nameof(messageManagerService));
        _entityConverter = entityConverter.IsNotNull(nameof(entityConverter));
        _logger = logger.IsNotNull(nameof(logger));
        _options = options.Value;
        _sqlSchemaService = sqlSchemaService.IsNotNull(nameof(sqlSchemaService));
        _customMetricsService = customMetricsService.IsNotNull(nameof(customMetricsService));
    }

    public async Task Sync(string entityLogicalName, ServiceBusReceivedMessage[] messages,
        ServiceBusMessageActions messageActions, CancellationToken ct)
    {
        var newMessages = new List<ServiceBusReceivedMessage>();
        var updatedMessages = new List<ServiceBusReceivedMessage>();
        var removedOrDeletedMessages = new List<ServiceBusReceivedMessage>();
        var associatedMessages = new List<ServiceBusReceivedMessage>();
        var disassociatedMessages = new List<ServiceBusReceivedMessage>();
        var shouldBeDeletedMessages = new List<ServiceBusReceivedMessage>();
        var shouldBeSkippedMessages = new List<ServiceBusReceivedMessage>();

        foreach (var message in messages)
        {
            if (ShouldBeDeleted(message))
            {
                shouldBeDeletedMessages.Add(message);
            }
            else
            {
                var requestType = message.GetRequestType();
                switch (requestType)
                {
                    case DynamicsRequestType.Create:
                        newMessages.Add(message);
                        break;
                    case DynamicsRequestType.Update:
                        updatedMessages.Add(message);
                        break;
                    case DynamicsRequestType.Delete:
                        removedOrDeletedMessages.Add(message);
                        break;
                    case DynamicsRequestType.Associate:
                        associatedMessages.Add(message);
                        break;
                    case DynamicsRequestType.Disassociate:
                        disassociatedMessages.Add(message);
                        break;
                    default:
                        break;
                }
            }
        }

        if (shouldBeDeletedMessages.Any())
        {
            await SendMessagesToDlq(shouldBeDeletedMessages, messageActions, ct);
        }
        if (newMessages.Any())
        {
            await ProcessNewMessages(entityLogicalName, newMessages, shouldBeSkippedMessages, messageActions, ct);
        }
        if (updatedMessages.Any())
        {
            await ProcessUpdatedMessages(entityLogicalName, updatedMessages, shouldBeSkippedMessages, messageActions, ct);
        }
        if (removedOrDeletedMessages.Any())
        {
            await ProcessRemovedOrDeletedMessages(entityLogicalName, removedOrDeletedMessages, shouldBeSkippedMessages, messageActions, ct);
        }
        if (associatedMessages.Any())
        {
            await ProcessAssociatedMessages(associatedMessages, messageActions, ct);
        }
        if (disassociatedMessages.Any())
        {
            await ProcessDisassociatedMessages(disassociatedMessages, messageActions, ct);
        }
        if (shouldBeSkippedMessages.Any())
        {
            await CompleteMessages(shouldBeSkippedMessages, messageActions, ct);
        }
    }

    private async Task BulkUpsertEntities(string entityLogicalName, List<MessageEntityMappingDto> messageEntityMappings,
        ServiceBusMessageActions messageActions, CancellationToken ct)
    {
        foreach (var mapping in messageEntityMappings)
        {
            var record = _entityConverter.ToDictionary(mapping.Entity);
            try
            {
                await _azureSqlService.UpsertAsync(entityLogicalName, record, ct);
                await messageActions.CompleteMessageAsync(mapping.Message, ct);

                _customMetricsService.SyncFromDataverseOperationLatencyMs(entityLogicalName,
                    ReplicationOperationType.Upsert.ToString(),
                    mapping.Message.ToRemoteExecutionContext().OperationCreatedOn.ToUniversalTime());
            }
            catch (Exception ex)
            {
                _logger.LogError("{ex}", ex);

                if (ex is TableNotExistException || ex is ColumnNotExistException)
                {
                    await HandleTableOrColumnNotExistSqlExceptions(entityLogicalName, ct);
                    await _azureSqlService.UpsertAsync(entityLogicalName, record, ct);
                    await messageActions.CompleteMessageAsync(mapping.Message, ct);

                    _customMetricsService.SyncFromDataverseOperationLatencyMs(entityLogicalName,
                        ReplicationOperationType.Upsert.ToString(),
                        mapping.Message.ToRemoteExecutionContext().OperationCreatedOn.ToUniversalTime());

                    continue;
                }

                var queueName = IsRelationshipEntity(entityLogicalName)
                    ? ServiceBusCustomQueueNames.AllRelationshipEntities
                    : entityLogicalName;

                await _serviceBusService.SendMessageAsync(queueName, mapping.Message, ct);
                await messageActions.CompleteMessageAsync(mapping.Message, ct);
            }
        }
    }

    private async Task BulkUpsertEntities(List<MessageAssociateRequestMappingDto> messageAssociateRequestMappings,
        ServiceBusMessageActions messageActions, CancellationToken ct)
    {
        foreach (var mapping in messageAssociateRequestMappings.Where(x => x.EntityNameAssociateRequestMapping != null))
        {
            var entityLogicalName = mapping.EntityNameAssociateRequestMapping.EntityLogicalName;
            var request = mapping.EntityNameAssociateRequestMapping.Request;
            var entities = _dynamicsService.RetrieveAssociateEntityResponse(entityLogicalName, request).Entities.ToList();
            foreach (var entity in entities)
            {
                var record = _entityConverter.ToDictionary(entity);
                try
                {
                    await _azureSqlService.UpsertAsync(entityLogicalName, record, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError("{ex}", ex);

                    var queueName = IsRelationshipEntity(entityLogicalName)
                        ? ServiceBusCustomQueueNames.AllRelationshipEntities
                        : entityLogicalName;

                    if (ex is TableNotExistException || ex is ColumnNotExistException)
                    {
                        await HandleTableOrColumnNotExistSqlExceptions(entityLogicalName, ct);
                        await _azureSqlService.UpsertAsync(entityLogicalName, record, ct);
                        continue;
                    }
                    await _serviceBusService.SendMessageAsync(queueName, mapping.Message, ct);
                    await messageActions.CompleteMessageAsync(mapping.Message, ct);
                    return;
                }
            }
            await messageActions.CompleteMessageAsync(mapping.Message, ct);

            _customMetricsService.SyncFromDataverseOperationLatencyMs(entityLogicalName,
                ReplicationOperationType.Upsert.ToString(),
                mapping.Message.ToRemoteExecutionContext().OperationCreatedOn.ToUniversalTime());
        }
    }

    private async Task BulkDeleteEntities(string entityLogicalName, List<MessageEntityReferenceMappingDto> messageEntityReferenceMappings,
        ServiceBusMessageActions messageActions, CancellationToken ct)
    {
        foreach (var mapping in messageEntityReferenceMappings)
        {
            var id = mapping.EntityReference.Id;
            try
            {
                await _azureSqlService.DeleteAsync(entityLogicalName, id, ct);
                await messageActions.CompleteMessageAsync(mapping.Message, ct);

                _customMetricsService.SyncFromDataverseOperationLatencyMs(entityLogicalName,
                    ReplicationOperationType.Delete.ToString(),
                    mapping.Message.ToRemoteExecutionContext().OperationCreatedOn.ToUniversalTime());
            }
            catch (Exception ex)
            {
                _logger.LogError("{ex}", ex);

                var queueName = IsRelationshipEntity(entityLogicalName)
                    ? ServiceBusCustomQueueNames.AllRelationshipEntities
                    : entityLogicalName;

                await _serviceBusService.SendMessageAsync(queueName, mapping.Message, ct);
                await messageActions.CompleteMessageAsync(mapping.Message, ct);
            }
        }
    }

    private async Task BulkDeleteEntities(List<MessageDisassociateRequestMappingDto> messageDisassociateRequestMappings,
        ServiceBusMessageActions messageActions, CancellationToken ct)
    {
        foreach (var mapping in messageDisassociateRequestMappings.Where(x => x.EntityNameDisassociateRequestMapping != null))
        {
            var entityLogicalName = mapping.EntityNameDisassociateRequestMapping.EntityLogicalName;
            var request = mapping.EntityNameDisassociateRequestMapping.Request;
            try
            {
                var records = request.ToDictionaryCollection();
                foreach (var record in records)
                {
                    await _azureSqlService.DeleteAsync(entityLogicalName, record, ct);
                }
                await messageActions.CompleteMessageAsync(mapping.Message, ct);

                _customMetricsService.SyncFromDataverseOperationLatencyMs(entityLogicalName,
                    ReplicationOperationType.Delete.ToString(),
                    mapping.Message.ToRemoteExecutionContext().OperationCreatedOn.ToUniversalTime());
            }
            catch (Exception ex)
            {
                _logger.LogError("{ex}", ex);

                var queueName = IsRelationshipEntity(entityLogicalName)
                    ? ServiceBusCustomQueueNames.AllRelationshipEntities
                    : entityLogicalName;

                await _serviceBusService.SendMessageAsync(queueName, mapping.Message, ct);
                await messageActions.CompleteMessageAsync(mapping.Message, ct);
            }
        }
    }

    private async Task HandleTableOrColumnNotExistSqlExceptions(string entityLogicalName, CancellationToken ct)
    {
        await _sqlSchemaService.CreateOrUpdateTableSchema(entityLogicalName, ct);
    }

    private bool ShouldBeDeleted(ServiceBusReceivedMessage message)
    {
        var attemptCount = message.GetAttemptCount();

        return attemptCount >= _options.MaxMessageRetryCount;
    }

    private static async Task SendMessagesToDlq(List<ServiceBusReceivedMessage> messages, ServiceBusMessageActions messageActions, CancellationToken ct)
    {
        foreach (var message in messages)
        {
            await messageActions.DeadLetterMessageAsync(message, cancellationToken: ct);
        }
    }

    private static async Task CompleteMessages(List<ServiceBusReceivedMessage> messages, ServiceBusMessageActions messageActions, CancellationToken ct)
    {
        foreach (var message in messages)
        {
            await messageActions.CompleteMessageAsync(message, cancellationToken: ct);
        }
    }

    private bool IsRelationshipEntity(string entityLogicalName)
    {
        var relationshipEntityLogicalNames = _messageManagerService.GetRelationshipEntityLogicalNames();

        return relationshipEntityLogicalNames.Contains(entityLogicalName);
    }

    private static List<MessageEntityMappingDto> SortEntitiesByModifiedOn(List<MessageEntityMappingDto> messageEntityMappings)
    {
        return messageEntityMappings.OrderBy(x => x.Entity.Id)
            .ThenBy(x => long.Parse(x.Entity.RowVersion))
            .ThenBy(x => x.Entity.Attributes[DynamicsCommonAttributeNames.ModifiedOn])
            .ToList();
    }

    private async Task ProcessNewMessages(string entityLogicalName, List<ServiceBusReceivedMessage> messages,
        List<ServiceBusReceivedMessage> shouldBeSkippedMessages, ServiceBusMessageActions messageActions, CancellationToken ct)
    {
        var messageEntityMappings = messages.Select(message =>
        {
            var entity = _messageManagerService.ProcessNewOrUpdatedMessage(message);
            if (entity == null)
            {
                shouldBeSkippedMessages.Add(message);
            }
            return new MessageEntityMappingDto { Message = message, Entity = entity };
        }).Where(dto => dto.Entity != null).ToList();
        await BulkUpsertEntities(entityLogicalName, messageEntityMappings, messageActions, ct);
    }

    private async Task ProcessUpdatedMessages(string entityLogicalName, List<ServiceBusReceivedMessage> messages,
        List<ServiceBusReceivedMessage> shouldBeSkippedMessages, ServiceBusMessageActions messageActions, CancellationToken ct)
    {
        var messageEntityMappings = messages.Select(message =>
        {
            var entity = _messageManagerService.ProcessNewOrUpdatedMessage(message);
            if (entity == null)
            {
                shouldBeSkippedMessages.Add(message);
            }
            return new MessageEntityMappingDto { Message = message, Entity = entity };
        }).Where(dto => dto.Entity != null).ToList();
        await BulkUpsertEntities(entityLogicalName, SortEntitiesByModifiedOn(messageEntityMappings), messageActions, ct);
    }

    private async Task ProcessRemovedOrDeletedMessages(string entityLogicalName, List<ServiceBusReceivedMessage> messages,
        List<ServiceBusReceivedMessage> shouldBeSkippedMessages, ServiceBusMessageActions messageActions, CancellationToken ct)
    {
        var messageEntityReferenceMappings = messages.Select(message =>
        {
            var entityReference = _messageManagerService.ProcessRemovedOrDeletedMessage(message);
            if (entityReference == null)
            {
                shouldBeSkippedMessages.Add(message);
            }
            return new MessageEntityReferenceMappingDto { Message = message, EntityReference = entityReference };
        }).Where(dto => dto.EntityReference != null).ToList();
        await BulkDeleteEntities(entityLogicalName, messageEntityReferenceMappings, messageActions, ct);
    }

    private async Task ProcessAssociatedMessages(List<ServiceBusReceivedMessage> messages,
        ServiceBusMessageActions messageActions, CancellationToken ct)
    {
        var messageAssociateRequestMappings = messages.Select(message =>
        {
            return new MessageAssociateRequestMappingDto
            {
                Message = message,
                EntityNameAssociateRequestMapping = _messageManagerService.ProcessAssociatedMessage(message)
            };
        }).ToList();
        await BulkUpsertEntities(messageAssociateRequestMappings, messageActions, ct);
    }

    private async Task ProcessDisassociatedMessages(List<ServiceBusReceivedMessage> messages,
        ServiceBusMessageActions messageActions, CancellationToken ct)
    {
        var messageDisassociateRequestMappings = messages.Select(message =>
        {
            return new MessageDisassociateRequestMappingDto
            {
                Message = message,
                EntityNameDisassociateRequestMapping = _messageManagerService.ProcessDisassociatedMessage(message)
            };
        }).ToList();
        await BulkDeleteEntities(messageDisassociateRequestMappings, messageActions, ct);
    }
}

