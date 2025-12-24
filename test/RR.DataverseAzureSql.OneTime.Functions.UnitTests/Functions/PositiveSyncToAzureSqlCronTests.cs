using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Functions.Worker;
using RR.DataverseAzureSql.Common.Constants;
using RR.DataverseAzureSql.Services.Services.Telemetry;
using RR.DataverseAzureSql.Tests.Common.Factories;
using RR.DataverseAzureSql.Tests.Common.Mocks.Telemetry;
using Xunit;

namespace RR.DataverseAzureSql.OneTime.Functions.UnitTests.Functions
{
    public class PositiveSyncToAzureSqlCronTests : PositiveSyncToAzureSqlTestBase
    {
        private readonly TelemetryConfiguration _fakeTelemetryConfiguration;
        private readonly CustomMetricsService _customMetricsService;

        private readonly List<ITelemetry> _telemetryItems = new List<ITelemetry>();

        public PositiveSyncToAzureSqlCronTests()
        {
            _fakeTelemetryConfiguration = FakeTelemetryConfiguration.Create(_telemetryItems);
            _customMetricsService = EntityFactory.CreateCustomMetricsService(_fakeTelemetryConfiguration);
        }

        [Fact]
        public async Task WhenCronRun_ThenData_ShouldBeInsertedToSql()
        {
            // Arrange
            var functionContext = EntityFactory.CreateContext();
            using var sqlConnectionFactory = EntityFactory.CreateSqlExpressDbConnectionFactory();
            var workOrder = GetWorkOrder();
            var entityAttributesMetadataService = GetDynamicsEntityAttributesMetadataService(workOrder);
            var dynamicsService = GetDynamicsService(entityAttributesMetadataService, workOrder, null);
            var durableClient = CreateDurableTaskClient(sqlConnectionFactory, dynamicsService, entityAttributesMetadataService);
            var oneTimeSyncToAzureSqlServiceOptionsOptions = GetOneTimeSyncToAzureSqlServiceOptions();
            var function = EntityFactory.CreateSyncToAzureSqlCron(_customMetricsService, oneTimeSyncToAzureSqlServiceOptionsOptions);
            // Act
            await function.Run(new TimerInfo(), durableClient, functionContext);
            // Assert
            await AssertEntity(EntityLogicalNames.WorkOrder, sqlConnectionFactory, workOrder);
        }

        [Fact]
        public async Task WhenCronRun_ThenData_ShouldBeUpdatedInSql()
        {
            // Arrange
            var functionContext = EntityFactory.CreateContext();
            using var sqlConnectionFactory = EntityFactory.CreateSqlExpressDbConnectionFactory();
            var workOrder = GetWorkOrder();
            var entityAttributesMetadataService = GetDynamicsEntityAttributesMetadataService(workOrder);
            var dynamicsService = GetDynamicsService(entityAttributesMetadataService, workOrder, null);
            var durableClient = CreateDurableTaskClient(sqlConnectionFactory, dynamicsService, entityAttributesMetadataService);
            var oneTimeSyncToAzureSqlServiceOptionsOptions = GetOneTimeSyncToAzureSqlServiceOptions();
            var function = EntityFactory.CreateSyncToAzureSqlCron(_customMetricsService, oneTimeSyncToAzureSqlServiceOptionsOptions);
            // Create
            await function.Run(new TimerInfo(), durableClient, functionContext);
            // Change workorder
            ChangeAttributes(workOrder);
            var dynamicsService2 = GetDynamicsService(entityAttributesMetadataService, workOrder, null);

            var durableClient2 = CreateDurableTaskClient(sqlConnectionFactory, dynamicsService, entityAttributesMetadataService);
            var function2 = EntityFactory.CreateSyncToAzureSqlCron(_customMetricsService, oneTimeSyncToAzureSqlServiceOptionsOptions);
            // Update workOrder
            await function2.Run(new TimerInfo(), durableClient2, functionContext);
            // Assert
            await AssertEntity(EntityLogicalNames.WorkOrder, sqlConnectionFactory, workOrder);
        }

        [Fact]
        public async Task WhenCronRun_ThenData_ShouldBeDeletedFromSql()
        {
            // Arrange
            var functionContext = EntityFactory.CreateContext();
            using var sqlConnectionFactory = EntityFactory.CreateSqlExpressDbConnectionFactory();
            var workOrder = GetWorkOrder();
            var entityAttributesMetadataService = GetDynamicsEntityAttributesMetadataService(workOrder);
            var dynamicsService = GetDynamicsService(entityAttributesMetadataService, workOrder, null);
            var durableClient = CreateDurableTaskClient(sqlConnectionFactory, dynamicsService, entityAttributesMetadataService);
            var oneTimeSyncToAzureSqlServiceOptionsOptions = GetOneTimeSyncToAzureSqlServiceOptions();
            var function = EntityFactory.CreateSyncToAzureSqlCron(_customMetricsService, oneTimeSyncToAzureSqlServiceOptionsOptions);
            // Create
            await function.Run(new TimerInfo(), durableClient, functionContext);

            var dynamicsService2 = GetDynamicsService(entityAttributesMetadataService, null, workOrder);

            var durableClient2 = CreateDurableTaskClient(sqlConnectionFactory, dynamicsService2, entityAttributesMetadataService);
            var function2 = EntityFactory.CreateSyncToAzureSqlCron(_customMetricsService, oneTimeSyncToAzureSqlServiceOptionsOptions);

            // Delete workOrder
            await function2.Run(new TimerInfo(), durableClient2, functionContext);
            // Assert
            await AssertEntity(EntityLogicalNames.WorkOrder, sqlConnectionFactory, null);
        }
    }
}
