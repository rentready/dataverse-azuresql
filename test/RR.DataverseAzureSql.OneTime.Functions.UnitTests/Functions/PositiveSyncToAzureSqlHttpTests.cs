using RR.DataverseAzureSql.Common.Constants;
using RR.DataverseAzureSql.Common.Enums;
using RR.DataverseAzureSql.OneTime.Functions.Models.Requests;
using RR.DataverseAzureSql.Tests.Common.Factories;
using Xunit;

namespace RR.DataverseAzureSql.OneTime.Functions.UnitTests.Functions
{
    public class PositiveSyncToAzureSqlHttpTests : PositiveSyncToAzureSqlTestBase
    {
        [Fact]
        public async Task WhenManualRun_ThenData_ShouldBeInsertedToSql()
        {
            // Arrange
            var request = new SyncEntitiesRequest
            {
                Type = SyncType.Full,
                Entities = new List<string> { EntityLogicalNames.WorkOrder },
            };
            var httpRequestData = EntityFactory.CreateHttpRequestData(request);
            var functionContext = EntityFactory.CreateContext();
            using var sqlConnectionFactory = EntityFactory.CreateSqlExpressDbConnectionFactory();
            var workOrder = GetWorkOrder();
            var entityAttributesMetadataService = GetDynamicsEntityAttributesMetadataService(workOrder);
            var dynamicsService = GetDynamicsService(entityAttributesMetadataService, workOrder, null);
            var durableClient = CreateDurableTaskClient(sqlConnectionFactory, dynamicsService, entityAttributesMetadataService);
            var oneTimeSyncToAzureSqlServiceOptionsOptions = GetOneTimeSyncToAzureSqlServiceOptions();
            var function = EntityFactory.CreateSyncToAzureSqlHttp(oneTimeSyncToAzureSqlServiceOptionsOptions);

            // Act
            var httpResponse = await function.Run(httpRequestData, durableClient, functionContext);
            Assert.NotNull(httpResponse);
            await AssertEntity(EntityLogicalNames.WorkOrder, sqlConnectionFactory, workOrder);
        }

        [Fact]
        public async Task WhenManualRun_ThenData_ShouldBeUpdatedInSql()
        {
            // Arrange
            var request = new SyncEntitiesRequest
            {
                Type = SyncType.Full,
                Entities = new List<string> { EntityLogicalNames.WorkOrder },
            };
            var httpRequestData = EntityFactory.CreateHttpRequestData(request);
            var functionContext = EntityFactory.CreateContext();
            using var sqlConnectionFactory = EntityFactory.CreateSqlExpressDbConnectionFactory();
            var workOrder = GetWorkOrder();
            var entityAttributesMetadataService = GetDynamicsEntityAttributesMetadataService(workOrder);
            var dynamicsService = GetDynamicsService(entityAttributesMetadataService, workOrder, null);
            var durableClient = CreateDurableTaskClient(sqlConnectionFactory, dynamicsService, entityAttributesMetadataService);
            var oneTimeSyncToAzureSqlServiceOptionsOptions = GetOneTimeSyncToAzureSqlServiceOptions();
            var function = EntityFactory.CreateSyncToAzureSqlHttp(oneTimeSyncToAzureSqlServiceOptionsOptions);
            // Create
            var httpResponse = await function.Run(httpRequestData, durableClient, functionContext);
            // Change workorder
            ChangeAttributes(workOrder);
            var dynamicsService2 = GetDynamicsService(entityAttributesMetadataService, workOrder, null);

            var durableClient2 = CreateDurableTaskClient(sqlConnectionFactory, dynamicsService, entityAttributesMetadataService);
            var function2 = EntityFactory.CreateSyncToAzureSqlHttp(oneTimeSyncToAzureSqlServiceOptionsOptions);
            // Update workOrder
            var httpRequestData2 = EntityFactory.CreateHttpRequestData(request);
            var httpResponse2 = await function2.Run(httpRequestData2, durableClient2, functionContext);

            await AssertEntity(EntityLogicalNames.WorkOrder, sqlConnectionFactory, workOrder);
        }

        [Fact]
        public async Task WhenManualRun_ThenData_ShouldBeDeletedFromSql()
        {
            // Arrange
            var request = new SyncEntitiesRequest
            {
                Type = SyncType.Full,
                Entities = new List<string> { EntityLogicalNames.WorkOrder },
            };
            var httpRequestData = EntityFactory.CreateHttpRequestData(request);
            var functionContext = EntityFactory.CreateContext();
            using var sqlConnectionFactory = EntityFactory.CreateSqlExpressDbConnectionFactory();
            var workOrder = GetWorkOrder();
            var entityAttributesMetadataService = GetDynamicsEntityAttributesMetadataService(workOrder);
            var dynamicsService = GetDynamicsService(entityAttributesMetadataService, workOrder, null);
            var durableClient = CreateDurableTaskClient(sqlConnectionFactory, dynamicsService, entityAttributesMetadataService);
            var oneTimeSyncToAzureSqlServiceOptionsOptions = GetOneTimeSyncToAzureSqlServiceOptions();
            var function = EntityFactory.CreateSyncToAzureSqlHttp(oneTimeSyncToAzureSqlServiceOptionsOptions);
            // Create
            var httpResponse = await function.Run(httpRequestData, durableClient, functionContext);

            var dynamicsService2 = GetDynamicsService(entityAttributesMetadataService, null, workOrder);

            var durableClient2 = CreateDurableTaskClient(sqlConnectionFactory, dynamicsService2, entityAttributesMetadataService);
            var function2 = EntityFactory.CreateSyncToAzureSqlHttp(oneTimeSyncToAzureSqlServiceOptionsOptions);

            var httpRequestData2 = EntityFactory.CreateHttpRequestData(request);
            // Delete workOrder
            var httpResponse2 = await function2.Run(httpRequestData2, durableClient2, functionContext);

            await AssertEntity(EntityLogicalNames.WorkOrder, sqlConnectionFactory, null);
        }
    }
}
