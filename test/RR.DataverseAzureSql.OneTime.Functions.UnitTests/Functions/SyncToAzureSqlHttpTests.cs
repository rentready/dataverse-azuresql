using System.Net;
using FakeItEasy;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using RR.DataverseAzureSql.Common.Constants;
using RR.DataverseAzureSql.Common.Enums;
using RR.DataverseAzureSql.OneTime.Functions.Models.Requests;
using RR.DataverseAzureSql.Tests.Common.Extensions;
using RR.DataverseAzureSql.Tests.Common.Factories;
using RR.DataverseAzureSql.Tests.Common.Mocks;
using Xunit;

namespace RR.DataverseAzureSql.OneTime.Functions.UnitTests.Functions
{
    public class SyncToAzureSqlHttpTests
    {
        [Fact]
        public async Task IfEntitiesIsEmpty_Then_ItShouldBeReceivedFromOptions()
        {
            // Arrange
            var expectedEntityLogicalNames = $"{EntityLogicalNames.Uom},{EntityLogicalNames.TimeEntry}";
            var options = EntityFactory.CreateOneTimeSyncToAzureSqlServiceOptions(expectedEntityLogicalNames);
            var function = EntityFactory.CreateSyncToAzureSqlHttp(options);
            var request = new SyncEntitiesRequest
            {
                Type = SyncType.Full,
                Entities = null,
            };

            SyncEntitiesRequest actualRequest = null;
            var httpRequestData = EntityFactory.CreateHttpRequestData(request);
            var durableClient = A.Fake<DurableTaskClient>();
            var functionContext = EntityFactory.CreateContext();

            A.CallTo(() => durableClient.ScheduleNewOrchestrationInstanceAsync(A<TaskName>.Ignored, A<object>.Ignored,
                    A<StartOrchestrationOptions>.Ignored, A<CancellationToken>.Ignored))
                .ReturnsLazily((TaskName orchestratorName, object request, StartOrchestrationOptions options,
                    CancellationToken ct) =>
                {
                    actualRequest = request as SyncEntitiesRequest;
                    return Task.FromResult("id");
                });

            // Act
            var httpResponse = await function.Run(httpRequestData, durableClient, functionContext);
            Assert.NotNull(httpResponse);

            Assert.NotNull(actualRequest);
            Assert.Equal(request.Type, actualRequest.Type);
            Assert.Equal(2, actualRequest.Entities.Count);
            Assert.Equal(EntityLogicalNames.Uom, actualRequest.Entities[0]);
            Assert.Equal(EntityLogicalNames.TimeEntry, actualRequest.Entities[1]);
        }

        [Fact]
        public async Task IfRequestIsIncorrect_Then_BadRequestShouldBeReturned()
        {
            // Arrange
            var expectedEntityLogicalNames = $"{EntityLogicalNames.Uom}";
            var options = EntityFactory.CreateOneTimeSyncToAzureSqlServiceOptions(expectedEntityLogicalNames);
            var function = EntityFactory.CreateSyncToAzureSqlHttp(options);
            var request = new SyncEntitiesRequest
            {
                Entities = null,
            };
            var httpRequestData = EntityFactory.CreateHttpRequestData(request);
            var durableClient = A.Fake<DurableTaskClient>();
            var functionContext = EntityFactory.CreateContext();
            // Act
            var httpResponse = await function.Run(httpRequestData, durableClient, functionContext);
            Assert.NotNull(httpResponse);

            var stringResponse = httpResponse.ReadStringFromBody();
            Assert.Contains("Model is invalid:", stringResponse);
            Assert.Contains($"The field {nameof(SyncEntitiesRequest.Type)} is invalid", stringResponse);
            Assert.Equal(HttpStatusCode.BadRequest, httpResponse.StatusCode);
        }

        [Fact]
        public async Task IfInstanceAlreadyStarted_Then_Conflict409ShouldBeReturned()
        {
            // Arrange
            var expectedEntityLogicalNames = $"{EntityLogicalNames.Uom}";
            var options = EntityFactory.CreateOneTimeSyncToAzureSqlServiceOptions(expectedEntityLogicalNames);
            var function = EntityFactory.CreateSyncToAzureSqlHttp(options);
            var request = new SyncEntitiesRequest
            {
                Type = SyncType.Changes,
                Entities = null,
            };
            var httpRequestData = EntityFactory.CreateHttpRequestData(request);
            var durableClient = A.Fake<DurableTaskClient>();
            var functionContext = EntityFactory.CreateContext();
            var instanceId = request.Type.ToString();
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

            A.CallTo(() => durableClient.GetInstancesAsync(A<string>.Ignored, A<bool>.Ignored, A<CancellationToken>.Ignored)).Returns(Task.FromResult(meatadata));

            // Act
            var httpResponse = await function.Run(httpRequestData, durableClient, functionContext);
            Assert.NotNull(httpResponse);

            var stringResponse = httpResponse.ReadStringFromBody();
            Assert.Contains("statusQueryGetUri", stringResponse);
            Assert.Contains("terminatePostUri", stringResponse);
            Assert.Equal(HttpStatusCode.Conflict, httpResponse.StatusCode);
        }
    }
}
