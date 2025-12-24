using System.Data.SqlClient;
using Microsoft.Extensions.Logging;
using RR.Common.Validations;
using RR.DataverseAzureSql.Common.Exceptions;
using RR.DataverseAzureSql.Common.Interfaces.Services.AzureSql;
using RR.DataverseAzureSql.Common.Interfaces.Services.Db;

namespace RR.DataverseAzureSql.Services.Services.AzureSql;

public class AzureSqlService : IAzureSqlService
{
    private readonly IQueryBuilderService _queryBuilderService;
    private readonly ILogger<AzureSqlService> _logger;
    private readonly ISqlConnectionFactory _connectionFactory;

    public AzureSqlService(IQueryBuilderService queryBuilderService,
        ISqlConnectionFactory connectionFactory,
        ILogger<AzureSqlService> logger)
    {
        _queryBuilderService = queryBuilderService.IsNotNull(nameof(queryBuilderService));
        _logger = logger.IsNotNull(nameof(logger));
        _connectionFactory = connectionFactory.IsNotNull(nameof(connectionFactory));
    }

    public async Task UpsertAsync(string entityLogicalName, Dictionary<string, object> record, CancellationToken ct)
    {
        var statement = _queryBuilderService.GetUpsertEntityStatement(entityLogicalName, record);
        try
        {
            using var connection = _connectionFactory.Create();
            using var command = new SqlCommand(statement, connection);
            await connection.OpenAsync(ct);

            var count = await command.ExecuteNonQueryAsync(ct);
            if (count == 0)
            {
                _logger.LogWarning("RowAffect: {count}, Command: {statement}", count, statement);

                command.CommandText = _queryBuilderService.RemoveVersionNumberCondition(statement);
                await command.ExecuteNonQueryAsync(ct);
            }
        }
        catch (SqlException ex)
        {
            _logger.LogError("[{entityLogicalName}]: {ex}", entityLogicalName, ex);
            // https://docs.microsoft.com/en-us/sql/relational-databases/errors-events/database-engine-events-and-errors
            switch (ex.Number)
            {
                case 207:
                    throw new ColumnNotExistException(entityLogicalName, "", ex);
                case 208:
                    throw new TableNotExistException(entityLogicalName, ex);
                default:
                    _logger.LogError("[{entityLogicalName}]: Type: {number} Exception: {ex}", entityLogicalName, ex.Number, ex);
                    throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("[{entityLogicalName}]: {ex}", entityLogicalName, ex);
            throw;
        }
    }

    public async Task DeleteAsync(string entityLogicalName, Dictionary<string, object> record, CancellationToken ct)
    {
        var statement = _queryBuilderService.GetDeleteEntityStatement(entityLogicalName, record);
        await DeleteAsync(entityLogicalName, statement, ct);
    }

    public async Task DeleteAsync(string entityLogicalName, Guid id, CancellationToken ct)
    {
        var statement = _queryBuilderService.GetDeleteEntityStatement(entityLogicalName, id);
        await DeleteAsync(entityLogicalName, statement, ct);
    }

    private async Task DeleteAsync(string entityLogicalName, string statement, CancellationToken ct)
    {
        try
        {
            using var connection = _connectionFactory.Create();
            using var command = new SqlCommand(statement, connection);
            await connection.OpenAsync(ct);

            await command.ExecuteNonQueryAsync(ct);
        }
        catch (SqlException ex)
        {
            _logger.LogError("[{entityLogicalName}]: {ex}", entityLogicalName, ex);
            // https://docs.microsoft.com/en-us/sql/relational-databases/errors-events/database-engine-events-and-errors
            switch (ex.Number)
            {
                case 207:
                    throw new ColumnNotExistException(entityLogicalName, "", ex);
                case 208:
                    throw new TableNotExistException(entityLogicalName, ex);
                default:
                    _logger.LogError("[{entityLogicalName}]: Type: {number} Exception: {ex}", entityLogicalName, ex.Number, ex);
                    throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("[{entityLogicalName}]: {ex}", entityLogicalName, ex);
            throw;
        }
    }
}

