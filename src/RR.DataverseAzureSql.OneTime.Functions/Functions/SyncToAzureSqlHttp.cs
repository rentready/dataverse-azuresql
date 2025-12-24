using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Options;
using RR.Common.Validations;
using RR.DataverseAzureSql.Common.Enums;
using RR.DataverseAzureSql.OneTime.Functions.Extensions;
using RR.DataverseAzureSql.OneTime.Functions.Functions.Durable;
using RR.DataverseAzureSql.OneTime.Functions.Models.Requests;
using RR.DataverseAzureSql.Services.Options.Services.Sync;

namespace RR.DataverseAzureSql.OneTime.Functions.Functions
{
    public class SyncToAzureSqlHttp
    {
        private readonly OneTimeSyncToAzureSqlServiceOptions _options;

        public SyncToAzureSqlHttp(IOptions<OneTimeSyncToAzureSqlServiceOptions> options)
        {
            _options = options.Value;
        }

        [Function(nameof(SyncToAzureSqlHttp))]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData httpRequest,
            [DurableClient] DurableTaskClient durableClient, FunctionContext executionContext)
        {
            var request = await httpRequest.ReadFromJsonAsync<SyncEntitiesRequest>(executionContext.CancellationToken);

            var validationResults = request.ValidateRequest();
            if (validationResults.Any())
            {
                return await httpRequest.ToBadRequest(validationResults);
            }

            var instanceFilter = new OrchestrationQuery(Statuses: new[] {
                OrchestrationRuntimeStatus.Running,
                OrchestrationRuntimeStatus.Pending,
                OrchestrationRuntimeStatus.Suspended
            });

            var result = durableClient.GetAllInstancesAsync(instanceFilter);
            if (result is not null)
            {
                await foreach (var item in result)
                {
                    if (item.InstanceId.Equals(SyncType.Full.ToString(), StringComparison.OrdinalIgnoreCase)
                        || item.InstanceId.Equals(SyncType.Changes.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        return durableClient.CreateCheckStatusResponse(httpRequest, item.InstanceId, HttpStatusCode.Conflict, executionContext.CancellationToken);
                    }
                }
            }

            if (request.Entities?.Any() != true)
            {
                request.Entities = _options.SynchronizedEntityLogicalNames
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .ToList();
            }
            var instanceId = await durableClient.ScheduleNewOrchestrationInstanceAsync(nameof(SyncToAzureSqlOrchestrator),
                    request, new StartOrchestrationOptions { InstanceId = request.Type.ToString() }, executionContext.CancellationToken);

            return durableClient.CreateCheckStatusResponse(httpRequest, instanceId, executionContext.CancellationToken);
        }
    }
}

