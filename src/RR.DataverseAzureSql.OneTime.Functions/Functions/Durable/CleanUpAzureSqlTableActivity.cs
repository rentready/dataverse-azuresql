using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using RR.Common.Validations;
using RR.DataverseAzureSql.Common.Interfaces.Services.AzureSql;

namespace RR.DataverseAzureSql.OneTime.Functions.Functions.Durable;

public class CleanUpAzureSqlTableActivity
{
    private readonly IAzureSqlFullSyncService _azureSqlFullSyncService;
    private readonly ILogger<CleanUpAzureSqlTableActivity> _logger;

    public CleanUpAzureSqlTableActivity(IAzureSqlFullSyncService syncToAzureSqlService,
        ILogger<CleanUpAzureSqlTableActivity> logger)
    {
        _azureSqlFullSyncService = syncToAzureSqlService.IsNotNull(nameof(syncToAzureSqlService));
        _logger = logger.IsNotNull(nameof(logger));
    }

    [Function(nameof(CleanUpAzureSqlTableActivity))]
    public async Task Run(
        [ActivityTrigger] string entityLogicalName, FunctionContext executionContext)
    {
        _logger.LogInformation("[{entityLogicalName}]: Going to clean up the table.", entityLogicalName);
        await _azureSqlFullSyncService.CleanUpTableAsync(entityLogicalName, executionContext.CancellationToken);
    }
}

