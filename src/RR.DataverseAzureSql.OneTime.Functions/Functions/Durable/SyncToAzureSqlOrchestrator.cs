using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using RR.DataverseAzureSql.OneTime.Functions.Dtos;
using RR.DataverseAzureSql.OneTime.Functions.Models.Requests;

namespace RR.DataverseAzureSql.OneTime.Functions.Functions.Durable;

public class SyncToAzureSqlOrchestrator
{
    [Function(nameof(SyncToAzureSqlOrchestrator))]
    public async Task Run(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var input = context.GetInput<SyncEntitiesRequest>();

        var concurrentTasks = new List<Task>();
        foreach (string entity in input.Entities)
        {
            var workerInput = new WorkerInputDto
            {
                Type = input.Type,
                EntityLogicalName = entity
            };
            var task = context.CallSubOrchestratorAsync(nameof(SyncToAzureSqlWorker),
                workerInput, new SubOrchestrationOptions { InstanceId = entity });

            concurrentTasks.Add(task);
        }

        await Task.WhenAll(concurrentTasks);
    }
}

