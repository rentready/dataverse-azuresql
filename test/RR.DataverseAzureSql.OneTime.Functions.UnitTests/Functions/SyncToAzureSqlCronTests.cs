using FakeItEasy;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using RR.DataverseAzureSql.Common.Constants;
using RR.DataverseAzureSql.Common.Enums;
using RR.DataverseAzureSql.Services.Services.Telemetry;
using RR.DataverseAzureSql.Tests.Common.Factories;
using RR.DataverseAzureSql.Tests.Common.Mocks;
using RR.DataverseAzureSql.Tests.Common.Mocks.Telemetry;
using Xunit;

namespace RR.DataverseAzureSql.OneTime.Functions.UnitTests.Functions
{
    public class SyncToAzureSqlCronTests
    {
        private readonly TelemetryConfiguration _fakeTelemetryConfiguration;
        private readonly CustomMetricsService _customMetricsService;

        private readonly List<ITelemetry> _telemetryItems = new List<ITelemetry>();

        public SyncToAzureSqlCronTests()
        {
            _fakeTelemetryConfiguration = FakeTelemetryConfiguration.Create(_telemetryItems);
            _customMetricsService = EntityFactory.CreateCustomMetricsService(_fakeTelemetryConfiguration);
        }

        [Theory]
        [InlineData("Full", false)]
        [InlineData("Changes", false)]
        [InlineData("AnyRandomId", true)]
        public async Task IfInstanceAlreadyStarted_Then_FunctionShouldNotBeRunForSpecificInstanceId(string instanceId, bool mustBeRun)
        {
            // Arrange
            var expectedEntityLogicalNames = $"{EntityLogicalNames.Uom}";
            var options = EntityFactory.CreateOneTimeSyncToAzureSqlServiceOptions(expectedEntityLogicalNames);
            var function = EntityFactory.CreateSyncToAzureSqlCron(_customMetricsService, options);

            var durableClient = A.Fake<DurableTaskClient>();
            var functionContext = EntityFactory.CreateContext();
            //var instanceId = request.Type.ToString();
            var meatadata = new OrchestrationMetadata(SyncType.Full.ToString(), instanceId)
            {
                RuntimeStatus = OrchestrationRuntimeStatus.Running
            };
            var pages = new Page<OrchestrationMetadata>[]
            {
                new Page<OrchestrationMetadata>(new OrchestrationMetadata[]
                    {
                        meatadata
                    })
            };

            A.CallTo(() => durableClient.GetAllInstancesAsync(A<OrchestrationQuery>.Ignored))
                .Returns(new FakeAsyncPageable<OrchestrationMetadata>(pages));

            A.CallTo(() => durableClient.GetInstancesAsync(A<string>.Ignored, A<bool>.Ignored, A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(meatadata));

            // Act
            await function.Run(new TimerInfo(), durableClient, functionContext);

            if (mustBeRun)
            {
                A.CallTo(() => durableClient.ScheduleNewOrchestrationInstanceAsync(A<TaskName>.Ignored, A<object>.Ignored,
                   A<StartOrchestrationOptions>.Ignored, A<CancellationToken>.Ignored))
               .MustHaveHappenedOnceExactly();
            }
            else
            {
                A.CallTo(() => durableClient.ScheduleNewOrchestrationInstanceAsync(A<TaskName>.Ignored, A<object>.Ignored,
                   A<StartOrchestrationOptions>.Ignored, A<CancellationToken>.Ignored))
               .MustNotHaveHappened();
            }
        }
    }
}
