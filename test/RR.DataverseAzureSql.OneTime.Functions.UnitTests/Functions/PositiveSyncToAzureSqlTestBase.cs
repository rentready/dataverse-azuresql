using FakeItEasy;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using RR.DataverseAzureSql.Common.Dtos;
using RR.DataverseAzureSql.Common.Interfaces.Services.Dynamics;
using RR.DataverseAzureSql.Common.Interfaces.Services.Metadata;
using RR.DataverseAzureSql.OneTime.Functions.Dtos;
using RR.DataverseAzureSql.OneTime.Functions.Functions.Durable;
using RR.DataverseAzureSql.OneTime.Functions.Models.Requests;
using RR.DataverseAzureSql.Tests.Common.Factories;
using RR.DataverseAzureSql.Tests.Common.Functions;
using RR.DataverseAzureSql.Tests.Common.Mocks.Databases;

namespace RR.DataverseAzureSql.OneTime.Functions.UnitTests.Functions
{
    public class PositiveSyncToAzureSqlTestBase : PositiveTestBase
    {
        protected DurableTaskClient CreateDurableTaskClient(SqlExpressDbConnectionFactory sqlConnectionFactory, IDynamicsService dynamicsService,
            IDynamicsEntityAttributesMetadataService entityAttributesMetadataService)
        {
            // options
            var oneTimeSyncToAzureSqlServiceOptionsOptions = GetOneTimeSyncToAzureSqlServiceOptions();
            var fullSyncOptions = EntityFactory.CreateAzureSqlFullSyncServiceOptions();

            // services            
            var azureSqlFullSyncService = EntityFactory.CreateAzureSqlFullSyncService(sqlConnectionFactory, fullSyncOptions);
            var createSqlSchemaService = EntityFactory.CreateSqlSchemaService(entityAttributesMetadataService, sqlConnectionFactory);

            var azureSqlChangesSyncService = EntityFactory.CreateAzureSqlChangesSyncService(sqlConnectionFactory);

            var oneTimeSyncToAzureSqlService = EntityFactory.CreateOneTimeSyncToAzureSqlService(dynamicsService, azureSqlFullSyncService, azureSqlChangesSyncService);

            // functions
            var syncToAzureSqlOrchestrator = EntityFactory.CreateSyncToAzureSqlOrchestrator();
            var createSyncToAzureSqlWorker = EntityFactory.CreateSyncToAzureSqlWorker(oneTimeSyncToAzureSqlServiceOptionsOptions);
            var cleanUpAzureSqlTableActivity = EntityFactory.CreateCleanUpAzureSqlTableActivity(azureSqlFullSyncService);
            var createOrUpdateAzureSqlTableSchemaActivity = EntityFactory.CreateCreateOrUpdateAzureSqlTableSchemaActivity(createSqlSchemaService);
            var syncToAzureSqlActivity = EntityFactory.CreateSyncToAzureSqlActivity(oneTimeSyncToAzureSqlService);

            // contexts
            var durableClient = A.Fake<DurableTaskClient>();
            var taskOrchestrationContext = A.Fake<TaskOrchestrationContext>();
            var taskSubOrchestrationContext = A.Fake<TaskOrchestrationContext>();
            var functionContext = EntityFactory.CreateContext();

            SyncEntitiesRequest syncEntitiesRequest = null;
            WorkerInputDto workerInputDto = null;

            A.CallTo(() => taskOrchestrationContext.GetInput<SyncEntitiesRequest>())
                .ReturnsLazily(() => syncEntitiesRequest);

            A.CallTo(() => taskSubOrchestrationContext.GetInput<WorkerInputDto>())
                .ReturnsLazily(() => workerInputDto);

            A.CallTo(() => durableClient.ScheduleNewOrchestrationInstanceAsync(A<TaskName>.Ignored, A<object>.Ignored,
                   A<StartOrchestrationOptions>.Ignored, A<CancellationToken>.Ignored))
               .ReturnsLazily(async (TaskName orchestratorName, object request, StartOrchestrationOptions options,
                   CancellationToken ct) =>
               {
                   syncEntitiesRequest = request as SyncEntitiesRequest;
                   await syncToAzureSqlOrchestrator.Run(taskOrchestrationContext);
                   return "id";
               });

            A.CallTo(() => taskOrchestrationContext.CallSubOrchestratorAsync(A<TaskName>.Ignored, A<object>.Ignored, A<TaskOptions>.Ignored))
                .ReturnsLazily((TaskName orchestratorName, object input, TaskOptions options) =>
                {
                    workerInputDto = input as WorkerInputDto;
                    return createSyncToAzureSqlWorker.Run(taskSubOrchestrationContext);
                });

            A.CallTo(() => taskSubOrchestrationContext.CallActivityAsync(A<TaskName>.Ignored, A<object>.Ignored,
                    A<TaskOptions>.Ignored))
                .ReturnsLazily((TaskName orchestratorName, object input, TaskOptions options) =>
                {
                    if (orchestratorName == nameof(CleanUpAzureSqlTableActivity))
                    {
                        return cleanUpAzureSqlTableActivity.Run((string)input, functionContext);
                    }
                    if (orchestratorName == nameof(CreateOrUpdateAzureSqlTableSchemaActivity))
                    {
                        return createOrUpdateAzureSqlTableSchemaActivity.Run((string)input, functionContext);
                    }
                    return Task.CompletedTask;
                });

            A.CallTo(() => taskSubOrchestrationContext.CallActivityAsync<EntitySyncStatusDto>(A<TaskName>.Ignored, A<object>.Ignored,
                   A<TaskOptions>.Ignored))
               .ReturnsLazily(async (TaskName orchestratorName, object input, TaskOptions options) =>
               {
                   return await syncToAzureSqlActivity.Run(input as EntitySyncStatusDto, functionContext);
               });

            return durableClient;
        }
    }
}
