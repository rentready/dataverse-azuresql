using System.Data.SqlClient;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RR.Common.Validations;
using RR.DataverseAzureSql.Common.Constants;
using RR.DataverseAzureSql.Common.Dtos;
using RR.DataverseAzureSql.Common.Extensions;
using RR.DataverseAzureSql.Common.Interfaces.Services.AzureSql;
using RR.DataverseAzureSql.Common.Interfaces.Services.Db;
using RR.DataverseAzureSql.Services.Options.Services.AzureSql;

namespace RR.DataverseAzureSql.Services.Services.AzureSql;

public class AzureSqlChangesSyncService : IAzureSqlChangesSyncService
{
    private readonly IQueryBuilderService _queryBuilderService;
    private readonly AzureSqlChangesSyncServiceOptions _changesSyncServiceOptions;
    private readonly ILogger<AzureSqlChangesSyncService> _logger;
    private readonly ISqlConnectionFactory _dbConnectionFactory;

    public AzureSqlChangesSyncService(IQueryBuilderService queryBuilderService,
        ISqlConnectionFactory dbConnectionFactory,
        IOptions<AzureSqlChangesSyncServiceOptions> changesSyncServiceOptions,
        ILogger<AzureSqlChangesSyncService> logger)
    {
        _queryBuilderService = queryBuilderService.IsNotNull(nameof(queryBuilderService));
        _logger = logger.IsNotNull(nameof(logger));
        _changesSyncServiceOptions = changesSyncServiceOptions.Value;
        _dbConnectionFactory = dbConnectionFactory.IsNotNull(nameof(dbConnectionFactory));
    }

    public async Task BulkUpsertAsync(string entityLogicalName, List<Dictionary<string, object>> records, CancellationToken ct)
    {
        using var connection = _dbConnectionFactory.Create();
        await connection.OpenAsync(ct);

        var container = await GroupRecords(connection, entityLogicalName, records, ct);

        using var transaction = connection.BeginTransaction();
        try
        {
            _logger.LogInformation("[{entityLogicalName}]: Start BulkUpsert", entityLogicalName);
            foreach (var batch in container.InsertedRecords.InBatchBy(_changesSyncServiceOptions.InsertBatchSize))
            {
                var statements = batch.Select(record =>
                    _queryBuilderService.GetInsertEntityStatement(entityLogicalName, record));

                await ExecuteNonQuery(connection, transaction, GroupStatements(statements), ct);
            }
            foreach (var batch in container.UpdatedRecords.InBatchBy(_changesSyncServiceOptions.UpdateBatchSize))
            {
                var statements = batch.Select(record =>
                    _queryBuilderService.GetUpdateEntityStatement(entityLogicalName, record));

                await ExecuteNonQuery(connection, transaction, GroupStatements(statements), ct);
            }

            await transaction.CommitAsync(ct);
            _logger.LogInformation("[{entityLogicalName}]: End BulkUpsert", entityLogicalName);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);

            _logger.LogError("[{entityLogicalName}]: {ex}", entityLogicalName, ex);
            throw;
        }
    }

    public async Task BulkDeleteAsync(string entityLogicalName, List<Guid> ids, CancellationToken ct)
    {
        using var connection = _dbConnectionFactory.Create();
        await connection.OpenAsync(ct);

        using var transaction = connection.BeginTransaction();
        try
        {
            _logger.LogInformation("[{entityLogicalName}]: Start BulkDelete", entityLogicalName);
            foreach (var batch in ids.InBatchBy(_changesSyncServiceOptions.DeleteBatchSize))
            {
                var statements = batch.Select(id =>
                    _queryBuilderService.GetDeleteEntityStatement(entityLogicalName, id));

                await ExecuteNonQuery(connection, transaction, GroupStatements(statements), ct);
            }

            await transaction.CommitAsync(ct);
            _logger.LogInformation("[{entityLogicalName}]: End BulkDelete", entityLogicalName);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);

            _logger.LogError("[{entityLogicalName}]: {ex}", entityLogicalName, ex);
            throw;
        }
    }

    private static async Task<AzureSqlSyncContainerDto> GroupRecords(SqlConnection connection, string entityLogicalName,
        List<Dictionary<string, object>> records, CancellationToken ct)
    {
        var container = new AzureSqlSyncContainerDto();
        var parameters = new string[records.Count];
        int i = 0;
        using var command = new SqlCommand();
        foreach (var record in records)
        {
            parameters[i] = $"@id{i}";
            command.Parameters.AddWithValue(parameters[i], record[AzureSqlCommonAttributeNames.Id]);
            i++;
        }
        var statement = $"SELECT {AzureSqlCommonAttributeNames.Id} FROM {entityLogicalName} (NOLOCK) " +
            $"WHERE {AzureSqlCommonAttributeNames.Id} IN ({string.Join(", ", parameters)});";
        command.CommandText = statement;
        command.Connection = connection;

        using var reader = await command.ExecuteReaderAsync(ct);

        var existedIds = new List<Guid>();
        while (await reader.ReadAsync(ct))
        {
            var id = reader.GetGuid(0);
            existedIds.Add(id);

            var item = records.First(x => Guid.Parse(x[AzureSqlCommonAttributeNames.Id].ToString().Trim('\'')) == id);
            container.UpdatedRecords.Add(item);
        }

        container.InsertedRecords = records.Where(x => !existedIds.Contains(Guid.Parse(x[AzureSqlCommonAttributeNames.Id].ToString().Trim('\'')))).ToList();

        return container;
    }

    private static string GroupStatements(IEnumerable<string> statements)
    {
        var cmdList = new StringBuilder();

        foreach (var statement in statements)
        {
            cmdList.AppendLine(statement);
        }

        return cmdList.ToString();
    }

    private static async Task ExecuteNonQuery(SqlConnection connection, SqlTransaction transaction, string statement, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(statement))
        {
            return;
        }

        using var command = new SqlCommand(statement, connection, transaction);
        await command.ExecuteNonQueryAsync(ct);
    }
}

