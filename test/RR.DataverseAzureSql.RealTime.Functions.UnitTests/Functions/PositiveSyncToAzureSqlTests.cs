using System.Data.SqlClient;
using Azure.Messaging.ServiceBus;
using FakeItEasy;
using Microsoft.Xrm.Sdk;
using RR.Common.Testing.Factories;
using RR.DataverseAzureSql.Common.Constants;
using RR.DataverseAzureSql.Common.Interfaces.Services.ServiceBus;
using RR.DataverseAzureSql.Tests.Common.Factories;
using RR.DataverseAzureSql.Tests.Common.Functions;
using Xunit;

namespace RR.DataverseAzureSql.RealTime.Functions.UnitTests.Functions;

public class PositiveSyncToAzureSqlTests : PositiveTestBase
{
    [Fact]
    public async Task WhenRealTimeRun_ThenData_ShouldBeInsertedToSql()
    {
        // Arrange
        using var sqlConnectionFactory = EntityFactory.CreateSqlExpressDbConnectionFactory();
        var workOrder = GetWorkOrder();
        var entityAttributesMetadataService = GetDynamicsEntityAttributesMetadataService(workOrder);
        var dynamicsService = GetDynamicsService(entityAttributesMetadataService, workOrder, null);
        var oneTimeSyncToAzureSqlServiceOptionsOptions = GetOneTimeSyncToAzureSqlServiceOptions();
        var azureSqlService = EntityFactory.CreateAzureSqlService(sqlConnectionFactory);
        var dynamicsEntityAttributesMetadataService = GetDynamicsEntityAttributesMetadataService();
        var createSqlSchemaService = EntityFactory.CreateSqlSchemaService(dynamicsEntityAttributesMetadataService, sqlConnectionFactory);
        var realTimeSyncToAzureSqlService = EntityFactory.CreateRealTimeSyncToAzureSqlService(dynamicsService, azureSqlService,
            sqlSchemaService: createSqlSchemaService);
        var function = EntityFactory.CreateSyncToAzureSql(realTimeSyncToAzureSqlService);
        var messageActions = EntityFactory.CreateFakeServiceBusMessageActions();
        var properties = new Dictionary<string, object>()
            {
                { "http://schemas.microsoft.com/xrm/2011/Claims/RequestName", "Create" }
            };
        var serviceBusMsg = EntityFactory.CreateFakeServiceBusReceivedMessage(Tests.Common.Properties.Resources.ServiceBusCreateMessage, properties);

        // Act
        await function.SyncToAzureSqlWorkOrder(new ServiceBusReceivedMessage[]
            { serviceBusMsg }, messageActions, default);
        // Assert
        Assert.Single(messageActions.CompletedMessages);
        await AssertEntity(EntityLogicalNames.WorkOrder, sqlConnectionFactory, workOrder);
    }

    [Fact]
    public async Task WhenRealTimeRun_ThenData_ShouldBeUpdatedInSql()
    {
        // Arrange
        using var sqlConnectionFactory = EntityFactory.CreateSqlExpressDbConnectionFactory();
        var workOrder = GetWorkOrder();
        var entityAttributesMetadataService = GetDynamicsEntityAttributesMetadataService(workOrder);
        var dynamicsService = GetDynamicsService(entityAttributesMetadataService, workOrder, null);
        var oneTimeSyncToAzureSqlServiceOptionsOptions = GetOneTimeSyncToAzureSqlServiceOptions();
        var azureSqlService = EntityFactory.CreateAzureSqlService(sqlConnectionFactory);
        var dynamicsEntityAttributesMetadataService = GetDynamicsEntityAttributesMetadataService();
        var createSqlSchemaService = EntityFactory.CreateSqlSchemaService(dynamicsEntityAttributesMetadataService, sqlConnectionFactory);
        var realTimeSyncToAzureSqlService = EntityFactory.CreateRealTimeSyncToAzureSqlService(dynamicsService, azureSqlService,
            sqlSchemaService: createSqlSchemaService);

        var function = EntityFactory.CreateSyncToAzureSql(realTimeSyncToAzureSqlService);
        var messageActions = EntityFactory.CreateFakeServiceBusMessageActions();
        var properties = new Dictionary<string, object>()
            {
                { "http://schemas.microsoft.com/xrm/2011/Claims/RequestName", "Create" }
            };
        var serviceBusMsg = EntityFactory.CreateFakeServiceBusReceivedMessage(Tests.Common.Properties.Resources.ServiceBusCreateMessage, properties);

        // Create
        await function.SyncToAzureSqlWorkOrder(new ServiceBusReceivedMessage[]
            { serviceBusMsg }, messageActions, default);
        // Change workorder
        var changedWorkOrder = GetWorkOrder(Tests.Common.Properties.Resources.ServiceBusUpdateMessage);
        var dynamicsEntityAttributesMetadataService2 = GetDynamicsEntityAttributesMetadataService(changedWorkOrder);
        var dynamicsService2 = GetDynamicsService(entityAttributesMetadataService, changedWorkOrder, null);

        var properties2 = new Dictionary<string, object>()
            {
                { "http://schemas.microsoft.com/xrm/2011/Claims/RequestName", "Update" }
            };
        var serviceBusMsg2 = EntityFactory.CreateFakeServiceBusReceivedMessage(Tests.Common.Properties.Resources.ServiceBusUpdateMessage, properties2);
        var createSqlSchemaService2 = EntityFactory.CreateSqlSchemaService(dynamicsEntityAttributesMetadataService2, sqlConnectionFactory);
        var realTimeSyncToAzureSqlService2 = EntityFactory.CreateRealTimeSyncToAzureSqlService(dynamicsService2, azureSqlService,
            sqlSchemaService: createSqlSchemaService2);
        var function2 = EntityFactory.CreateSyncToAzureSql(realTimeSyncToAzureSqlService2);

        // Update workOrder
        await function2.SyncToAzureSqlWorkOrder(new ServiceBusReceivedMessage[]
            { serviceBusMsg2 }, messageActions, default);

        Assert.Equal(2, messageActions.CompletedMessages.Count);
        await AssertEntity(EntityLogicalNames.WorkOrder, sqlConnectionFactory, changedWorkOrder, exludeSinkFields: true);
    }

    [Fact]
    public async Task WhenRealTimeRun_ThenData_ShouldBeDeletedFromSql()
    {
        // Arrange
        using var sqlConnectionFactory = EntityFactory.CreateSqlExpressDbConnectionFactory();
        var workOrder = GetWorkOrder();
        var entityAttributesMetadataService = GetDynamicsEntityAttributesMetadataService(workOrder);
        var dynamicsService = GetDynamicsService(entityAttributesMetadataService, workOrder, null);
        var oneTimeSyncToAzureSqlServiceOptionsOptions = GetOneTimeSyncToAzureSqlServiceOptions();
        var azureSqlService = EntityFactory.CreateAzureSqlService(sqlConnectionFactory);
        var dynamicsEntityAttributesMetadataService = GetDynamicsEntityAttributesMetadataService();
        var createSqlSchemaService = EntityFactory.CreateSqlSchemaService(dynamicsEntityAttributesMetadataService, sqlConnectionFactory);
        var realTimeSyncToAzureSqlService = EntityFactory.CreateRealTimeSyncToAzureSqlService(dynamicsService, azureSqlService, sqlSchemaService: createSqlSchemaService
            );

        var function = EntityFactory.CreateSyncToAzureSql(realTimeSyncToAzureSqlService);
        var messageActions = EntityFactory.CreateFakeServiceBusMessageActions();
        var properties = new Dictionary<string, object>()
            {
                { "http://schemas.microsoft.com/xrm/2011/Claims/RequestName", "Create" }
            };
        var serviceBusMsg = EntityFactory.CreateFakeServiceBusReceivedMessage(Tests.Common.Properties.Resources.ServiceBusCreateMessage, properties);

        // Create
        await function.SyncToAzureSqlWorkOrder(new ServiceBusReceivedMessage[]
            { serviceBusMsg }, messageActions, default);

        // Delete workorder
        var dynamicsEntityAttributesMetadataService2 = GetDynamicsEntityAttributesMetadataService(
            new Entity(EntityLogicalNames.WorkOrder));
        var dynamicsService2 = GetDynamicsService(entityAttributesMetadataService, null, workOrder);

        var properties2 = new Dictionary<string, object>()
            {
                { "http://schemas.microsoft.com/xrm/2011/Claims/RequestName", "Delete" }
            };
        var serviceBusMsg2 = EntityFactory.CreateFakeServiceBusReceivedMessage(Tests.Common.Properties.Resources.ServiceBusDeleteMessage, properties2);
        var createSqlSchemaService2 = EntityFactory.CreateSqlSchemaService(dynamicsEntityAttributesMetadataService2, sqlConnectionFactory);
        var realTimeSyncToAzureSqlService2 = EntityFactory.CreateRealTimeSyncToAzureSqlService(dynamicsService2, azureSqlService,
            sqlSchemaService: createSqlSchemaService2);
        var function2 = EntityFactory.CreateSyncToAzureSql(realTimeSyncToAzureSqlService2);

        // Delete workOrder
        await function2.SyncToAzureSqlWorkOrder(new ServiceBusReceivedMessage[]
            { serviceBusMsg2 }, messageActions, default);

        Assert.Equal(2, messageActions.CompletedMessages.Count);
        await AssertEntity(EntityLogicalNames.WorkOrder, sqlConnectionFactory, null);
    }

    [Fact]
    public async Task WhenRunAssociate_ThenData_ShouldBeInserted()
    {
        // Arrange
        var accountContactEntity = GetAccountContact();
        var xrmFakeContext = FakeContextFactory.Arrange(new[] { accountContactEntity });
        var orgService = xrmFakeContext.GetOrganizationService();
        var entityAttributesMetadataService = GetDynamicsEntityAttributesMetadataService(accountContactEntity);
        var dynamicsService = EntityFactory.CreateDynamicsService(orgService, entityAttributesMetadataService);
        using var sqlConnectionFactory = EntityFactory.CreateSqlExpressDbConnectionFactory();
        var oneTimeSyncToAzureSqlServiceOptionsOptions = GetOneTimeSyncToAzureSqlServiceOptions();
        var azureSqlService = EntityFactory.CreateAzureSqlService(sqlConnectionFactory);
        var dynamicsEntityAttributesMetadataService = GetDynamicsEntityAttributesMetadataService(accountContactEntity);
        var createSqlSchemaService = EntityFactory.CreateSqlSchemaService(dynamicsEntityAttributesMetadataService, sqlConnectionFactory);
        var serviceBusService = A.Fake<IServiceBusService>();
        var realTimeSyncToAzureSqlService = EntityFactory.CreateRealTimeSyncToAzureSqlService(dynamicsService, azureSqlService, sqlSchemaService: createSqlSchemaService, serviceBusService: serviceBusService);

        var function = EntityFactory.CreateSyncToAzureSql(realTimeSyncToAzureSqlService);
        var messageActions = EntityFactory.CreateFakeServiceBusMessageActions();
        var properties = new Dictionary<string, object>()
            {
                { "http://schemas.microsoft.com/xrm/2011/Claims/RequestName", "Associate" }
            };
        var serviceBusMsg = EntityFactory.CreateFakeServiceBusReceivedMessage(Tests.Common.Properties.Resources.ServiceBusAssociateMessage, properties);

        await function.SyncToAzureSqlWorkOrder(new ServiceBusReceivedMessage[]
            { serviceBusMsg }, messageActions, default);
        // Assert
        Assert.Single(messageActions.CompletedMessages);
        await AssertEntity(accountContactEntity.LogicalName, sqlConnectionFactory, accountContactEntity);
    }

    [Fact]
    public async Task WhenRunDisassociate_ThenData_ShouldBeDeleted()
    {
        // Arrange
        var accountContactEntity = GetAccountContact();
        var xrmFakeContext = FakeContextFactory.Arrange(new[] { accountContactEntity });
        var orgService = xrmFakeContext.GetOrganizationService();
        var entityAttributesMetadataService = GetDynamicsEntityAttributesMetadataService(accountContactEntity);
        var dynamicsService = EntityFactory.CreateDynamicsService(orgService, entityAttributesMetadataService);
        using var sqlConnectionFactory = EntityFactory.CreateSqlExpressDbConnectionFactory();
        var oneTimeSyncToAzureSqlServiceOptionsOptions = GetOneTimeSyncToAzureSqlServiceOptions();
        var azureSqlService = EntityFactory.CreateAzureSqlService(sqlConnectionFactory);
        var dynamicsEntityAttributesMetadataService = GetDynamicsEntityAttributesMetadataService(accountContactEntity);
        var createSqlSchemaService = EntityFactory.CreateSqlSchemaService(dynamicsEntityAttributesMetadataService, sqlConnectionFactory);
        var serviceBusService = A.Fake<IServiceBusService>();
        var realTimeSyncToAzureSqlService = EntityFactory.CreateRealTimeSyncToAzureSqlService(dynamicsService, azureSqlService, sqlSchemaService: createSqlSchemaService, serviceBusService: serviceBusService);

        var function = EntityFactory.CreateSyncToAzureSql(realTimeSyncToAzureSqlService);
        var messageActions = EntityFactory.CreateFakeServiceBusMessageActions();
        var properties = new Dictionary<string, object>()
            {
                { "http://schemas.microsoft.com/xrm/2011/Claims/RequestName", "Associate" }
            };
        var serviceBusMsg = EntityFactory.CreateFakeServiceBusReceivedMessage(Tests.Common.Properties.Resources.ServiceBusAssociateMessage, properties);

        // insert data
        await function.SyncToAzureSqlWorkOrder(new ServiceBusReceivedMessage[]
            { serviceBusMsg }, messageActions, default);

        var properties2 = new Dictionary<string, object>()
            {
                { "http://schemas.microsoft.com/xrm/2011/Claims/RequestName", "Disassociate" }
            };
        var serviceBusMsg2 = EntityFactory.CreateFakeServiceBusReceivedMessage(Tests.Common.Properties.Resources.ServiceBusDisassociateMessage, properties2);
        // delete data
        await function.SyncToAzureSqlWorkOrder(new ServiceBusReceivedMessage[]
           { serviceBusMsg2 }, messageActions, default);
        // Assert
        Assert.Equal(2, messageActions.CompletedMessages.Count);
        await AssertEntity(accountContactEntity.LogicalName, sqlConnectionFactory, null);
    }

    private static Entity GetAccountContact()
    {
        var entityLogicalName = "account_contact";
        var result = new Entity(entityLogicalName, Guid.NewGuid());
        var accountId = Guid.Parse("590ed509-1637-ec11-b6e6-000d3a5b2b42");
        result["accountid"] = accountId;
        var contactId = Guid.Parse("c12e1dcd-2f41-ec11-8c62-000d3a3436d6");
        result["contactid"] = contactId;
        return result;
    }
}

