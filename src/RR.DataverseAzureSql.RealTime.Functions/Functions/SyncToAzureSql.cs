using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using RR.Common.Validations;
using RR.DataverseAzureSql.Common.Constants;
using RR.DataverseAzureSql.Common.Interfaces.Services.Sync;

namespace RR.DataverseAzureSql.RealTime.Functions.Functions;

public class SyncToAzureSql
{
    private readonly IRealTimeSyncToAzureSqlService _syncToAzureSqlService;

    public SyncToAzureSql(IRealTimeSyncToAzureSqlService syncToAzureSqlService)
    {
        _syncToAzureSqlService = syncToAzureSqlService.IsNotNull(nameof(syncToAzureSqlService));
    }

    [Function(nameof(SyncToAzureSqlAccount))]
    public async Task SyncToAzureSqlAccount(
        [ServiceBusTrigger(EntityLogicalNames.Account,
            Connection = ConfigSectionNames.DataverseAzureSqlServiceBusConnectionString,
            IsBatched = true)] ServiceBusReceivedMessage [] messages, ServiceBusMessageActions messageActions, CancellationToken ct)
    {
        await Run(EntityLogicalNames.Account, messages, messageActions, ct);
    }

    [Function(nameof(SyncToAzureSqlBookableResource))]
    public async Task SyncToAzureSqlBookableResource(
        [ServiceBusTrigger(EntityLogicalNames.BookableResource,
            Connection = ConfigSectionNames.DataverseAzureSqlServiceBusConnectionString,
            IsBatched = true)] ServiceBusReceivedMessage [] messages, ServiceBusMessageActions messageActions, CancellationToken ct)
    {
        await Run(EntityLogicalNames.BookableResource, messages, messageActions, ct);
    }

    [Function(nameof(SyncToAzureSqlBookableResourceBooking))]
    public async Task SyncToAzureSqlBookableResourceBooking(
        [ServiceBusTrigger(EntityLogicalNames.BookableResourceBooking,
            Connection = ConfigSectionNames.DataverseAzureSqlServiceBusConnectionString,
            IsBatched = true)] ServiceBusReceivedMessage [] messages, ServiceBusMessageActions messageActions, CancellationToken ct)
    {
        await Run(EntityLogicalNames.BookableResourceBooking, messages, messageActions, ct);
    }

    [Function(nameof(SyncToAzureSqlContact))]
    public async Task SyncToAzureSqlContact(
        [ServiceBusTrigger(EntityLogicalNames.Contact,
            Connection = ConfigSectionNames.DataverseAzureSqlServiceBusConnectionString,
            IsBatched = true)] ServiceBusReceivedMessage [] messages, ServiceBusMessageActions messageActions, CancellationToken ct)
    {
        await Run(EntityLogicalNames.Contact, messages, messageActions, ct);
    }

    [Function(nameof(SyncToAzureSqlBookingAlert))]
    public async Task SyncToAzureSqlBookingAlert(
        [ServiceBusTrigger(EntityLogicalNames.BookingAlert,
            Connection = ConfigSectionNames.DataverseAzureSqlServiceBusConnectionString,
            IsBatched = true)] ServiceBusReceivedMessage [] messages, ServiceBusMessageActions messageActions, CancellationToken ct)
    {
        await Run(EntityLogicalNames.BookingAlert, messages, messageActions, ct);
    }

    [Function(nameof(SyncToAzureSqlWorkOrderService))]
    public async Task SyncToAzureSqlWorkOrderService(
        [ServiceBusTrigger(EntityLogicalNames.WorkOrderService,
            Connection = ConfigSectionNames.DataverseAzureSqlServiceBusConnectionString,
            IsBatched = true)] ServiceBusReceivedMessage [] messages, ServiceBusMessageActions messageActions, CancellationToken ct)
    {
        await Run(EntityLogicalNames.WorkOrderService, messages, messageActions, ct);
    }

    [Function(nameof(SyncToAzureSqlWorkOrder))]
    public async Task SyncToAzureSqlWorkOrder(
        [ServiceBusTrigger(EntityLogicalNames.WorkOrder,
            Connection = ConfigSectionNames.DataverseAzureSqlServiceBusConnectionString,
            IsBatched = true)] ServiceBusReceivedMessage [] messages, ServiceBusMessageActions messageActions, CancellationToken ct)
    {
        await Run(EntityLogicalNames.WorkOrder, messages, messageActions, ct);
    }

    [Function(nameof(SyncToAzureSqlIncident))]
    public async Task SyncToAzureSqlIncident(
        [ServiceBusTrigger(EntityLogicalNames.Incident,
            Connection = ConfigSectionNames.DataverseAzureSqlServiceBusConnectionString,
            IsBatched = true)] ServiceBusReceivedMessage [] messages, ServiceBusMessageActions messageActions, CancellationToken ct)
    {
        await Run(EntityLogicalNames.Incident, messages, messageActions, ct);
    }

    [Function(nameof(SyncToAzureSqlPriceLevel))]
    public async Task SyncToAzureSqlPriceLevel(
        [ServiceBusTrigger(EntityLogicalNames.PriceLevel,
            Connection = ConfigSectionNames.DataverseAzureSqlServiceBusConnectionString,
            IsBatched = true)] ServiceBusReceivedMessage [] messages, ServiceBusMessageActions messageActions, CancellationToken ct)
    {
        await Run(EntityLogicalNames.PriceLevel, messages, messageActions, ct);
    }

    [Function(nameof(SyncToAzureSqlBookingStatus))]
    public async Task SyncToAzureSqlBookingStatus(
        [ServiceBusTrigger(EntityLogicalNames.BookingStatus,
            Connection = ConfigSectionNames.DataverseAzureSqlServiceBusConnectionString,
            IsBatched = true)] ServiceBusReceivedMessage [] messages, ServiceBusMessageActions messageActions, CancellationToken ct)
    {
        await Run(EntityLogicalNames.BookingStatus, messages, messageActions, ct);
    }

    [Function(nameof(SyncToAzureSqlOrganizationalUnit))]
    public async Task SyncToAzureSqlOrganizationalUnit(
        [ServiceBusTrigger(EntityLogicalNames.OrganizationalUnit,
            Connection = ConfigSectionNames.DataverseAzureSqlServiceBusConnectionString,
            IsBatched = true)] ServiceBusReceivedMessage [] messages, ServiceBusMessageActions messageActions, CancellationToken ct)
    {
        await Run(EntityLogicalNames.OrganizationalUnit, messages, messageActions, ct);
    }

    [Function(nameof(SyncToAzureSqlProduct))]
    public async Task SyncToAzureSqlProduct(
        [ServiceBusTrigger(EntityLogicalNames.Product,
            Connection = ConfigSectionNames.DataverseAzureSqlServiceBusConnectionString,
            IsBatched = true)] ServiceBusReceivedMessage [] messages, ServiceBusMessageActions messageActions, CancellationToken ct)
    {
        await Run(EntityLogicalNames.Product, messages, messageActions, ct);
    }

    [Function(nameof(SyncToAzureSqlRequirementResourcePreference))]
    public async Task SyncToAzureSqlRequirementResourcePreference(
        [ServiceBusTrigger(EntityLogicalNames.RequirementResourcePreference,
            Connection = ConfigSectionNames.DataverseAzureSqlServiceBusConnectionString,
            IsBatched = true)] ServiceBusReceivedMessage [] messages, ServiceBusMessageActions messageActions, CancellationToken ct)
    {
        await Run(EntityLogicalNames.RequirementResourcePreference, messages, messageActions, ct);
    }

    [Function(nameof(SyncToAzureSqlWorkOrderType))]
    public async Task SyncToAzureSqlWorkOrderType(
        [ServiceBusTrigger(EntityLogicalNames.WorkOrderType,
            Connection = ConfigSectionNames.DataverseAzureSqlServiceBusConnectionString,
            IsBatched = true)] ServiceBusReceivedMessage [] messages, ServiceBusMessageActions messageActions, CancellationToken ct)
    {
        await Run(EntityLogicalNames.WorkOrderType, messages, messageActions, ct);
    }

    [Function(nameof(SyncToAzureSqlTimeOffRequest))]
    public async Task SyncToAzureSqlTimeOffRequest(
        [ServiceBusTrigger(EntityLogicalNames.TimeOffRequest,
            Connection = ConfigSectionNames.DataverseAzureSqlServiceBusConnectionString,
            IsBatched = true)] ServiceBusReceivedMessage [] messages, ServiceBusMessageActions messageActions, CancellationToken ct)
    {
        await Run(EntityLogicalNames.TimeOffRequest, messages, messageActions, ct);
    }

    [Function(nameof(SyncToAzureSqlProductPriceLevel))]
    public async Task SyncToAzureSqlProductPriceLevel(
        [ServiceBusTrigger(EntityLogicalNames.ProductPriceLevel,
            Connection = ConfigSectionNames.DataverseAzureSqlServiceBusConnectionString,
            IsBatched = true)] ServiceBusReceivedMessage [] messages, ServiceBusMessageActions messageActions, CancellationToken ct)
    {
        await Run(EntityLogicalNames.ProductPriceLevel, messages, messageActions, ct);
    }

    [Function(nameof(SyncToAzureSqlTimeEntry))]
    public async Task SyncToAzureSqlTimeEntry(
        [ServiceBusTrigger(EntityLogicalNames.TimeEntry,
            Connection = ConfigSectionNames.DataverseAzureSqlServiceBusConnectionString,
            IsBatched = true)] ServiceBusReceivedMessage [] messages, ServiceBusMessageActions messageActions, CancellationToken ct)
    {
        await Run(EntityLogicalNames.TimeEntry, messages, messageActions, ct);
    }

    [Function(nameof(SyncToAzureSqlUom))]
    public async Task SyncToAzureSqlUom(
        [ServiceBusTrigger(EntityLogicalNames.Uom,
            Connection = ConfigSectionNames.DataverseAzureSqlServiceBusConnectionString,
            IsBatched = true)] ServiceBusReceivedMessage [] messages, ServiceBusMessageActions messageActions, CancellationToken ct)
    {
        await Run(EntityLogicalNames.Uom, messages, messageActions, ct);
    }

    [Function(nameof(SyncToAzureSqlResourceRequirement))]
    public async Task SyncToAzureSqlResourceRequirement(
        [ServiceBusTrigger(EntityLogicalNames.ResourceRequirement,
            Connection = ConfigSectionNames.DataverseAzureSqlServiceBusConnectionString,
            IsBatched = true)] ServiceBusReceivedMessage [] messages, ServiceBusMessageActions messageActions, CancellationToken ct)
    {
        await Run(EntityLogicalNames.ResourceRequirement, messages, messageActions, ct);
    }

    [Function(nameof(SyncToAzureSqlAnnotation))]
    public async Task SyncToAzureSqlAnnotation(
        [ServiceBusTrigger(EntityLogicalNames.Annotation,
            Connection = ConfigSectionNames.DataverseAzureSqlServiceBusConnectionString,
            IsBatched = true)] ServiceBusReceivedMessage [] messages, ServiceBusMessageActions messageActions, CancellationToken ct)
    {
        await Run(EntityLogicalNames.Annotation, messages, messageActions, ct);
    }

    [Function(nameof(SyncToAzureSqlSystemUser))]
    public async Task SyncToAzureSqlSystemUser(
        [ServiceBusTrigger(EntityLogicalNames.SystemUser,
            Connection = ConfigSectionNames.DataverseAzureSqlServiceBusConnectionString,
            IsBatched = true)] ServiceBusReceivedMessage [] messages, ServiceBusMessageActions messageActions, CancellationToken ct)
    {
        await Run(EntityLogicalNames.SystemUser, messages, messageActions, ct);
    }

    [Function(nameof(SyncToAzureSqlAllRelationshipEntities))]
    public async Task SyncToAzureSqlAllRelationshipEntities(
        [ServiceBusTrigger(ServiceBusCustomQueueNames.AllRelationshipEntities,
            Connection = ConfigSectionNames.DataverseAzureSqlServiceBusConnectionString,
            IsBatched = true)] ServiceBusReceivedMessage [] messages, ServiceBusMessageActions messageActions, CancellationToken ct)
    {
        await Run(ServiceBusCustomQueueNames.AllRelationshipEntities, messages, messageActions, ct);
    }

    private async Task Run(string entityLogicalName, ServiceBusReceivedMessage[] messages, ServiceBusMessageActions messageActions, CancellationToken ct)
    {
        await _syncToAzureSqlService.Sync(entityLogicalName, messages, messageActions, ct);
    }
}

