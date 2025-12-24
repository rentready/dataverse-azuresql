using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using RR.Common.Validations;
using RR.DataverseAzureSql.Common.Interfaces.Services.AzureSql;

namespace RR.DataverseAzureSql.OneTime.Functions.Functions.Durable;

public class CreateOrUpdateAzureSqlTableSchemaActivity
{
    private readonly ISqlSchemaService _sqlSchemaService;
    private readonly ILogger<CreateOrUpdateAzureSqlTableSchemaActivity> _logger;

    public CreateOrUpdateAzureSqlTableSchemaActivity(ISqlSchemaService sqlSchemaService,
        ILogger<CreateOrUpdateAzureSqlTableSchemaActivity> logger)
    {
        _sqlSchemaService = sqlSchemaService.IsNotNull(nameof(sqlSchemaService));
        _logger = logger.IsNotNull(nameof(logger));
    }

    [Function(nameof(CreateOrUpdateAzureSqlTableSchemaActivity))]
    public async Task Run(
        [ActivityTrigger] string entityLogicalName, FunctionContext executionContext)
    {
        _logger.LogInformation("[{entityLogicalName}]: Going to create/update the table schema.", entityLogicalName);
        await _sqlSchemaService.CreateOrUpdateTableSchema(entityLogicalName, executionContext.CancellationToken);
    }
}

