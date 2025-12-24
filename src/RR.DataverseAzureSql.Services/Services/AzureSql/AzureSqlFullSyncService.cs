using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RR.Common.Validations;
using RR.DataverseAzureSql.Common.Constants;
using RR.DataverseAzureSql.Common.Interfaces.Services.AzureSql;
using RR.DataverseAzureSql.Common.Interfaces.Services.Db;
using RR.DataverseAzureSql.Services.Options.Services.AzureSql;

namespace RR.DataverseAzureSql.Services.Services.AzureSql;

public class AzureSqlFullSyncService : IAzureSqlFullSyncService
{
    private readonly IQueryBuilderService _queryBuilderService;
    private readonly AzureSqlFullSyncServiceOptions _fullSyncServiceOptions;
    private readonly ILogger<AzureSqlFullSyncService> _logger;
    private readonly ISqlConnectionFactory _dbConnectionFactory;

    public AzureSqlFullSyncService(IQueryBuilderService queryBuilderService,
        ISqlConnectionFactory dbConnectionFactory,
        IOptions<AzureSqlFullSyncServiceOptions> fullSyncServiceOptions,
        ILogger<AzureSqlFullSyncService> logger)
    {
        _queryBuilderService = queryBuilderService.IsNotNull(nameof(queryBuilderService));
        _logger = logger.IsNotNull(nameof(logger));
        _fullSyncServiceOptions = fullSyncServiceOptions.Value.IsNotNull(nameof(fullSyncServiceOptions));
        _dbConnectionFactory = dbConnectionFactory.IsNotNull(nameof(dbConnectionFactory));
    }

    public async Task BulkCopyAsync(string entityLogicalName, List<Dictionary<string, object>> records, CancellationToken ct)
    {
        var dataTable = GetDataTable(records);

        using var connection = _dbConnectionFactory.Create();
        await connection.OpenAsync(ct);

        var transaction = await connection.BeginTransactionAsync(ct);
        try
        {
            using var sqlBulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, (SqlTransaction)transaction);
            SetupBulkCopy(sqlBulkCopy, entityLogicalName, dataTable);

            _logger.LogInformation("[{entityLogicalName}]: Start BulkCopy", entityLogicalName);
            await sqlBulkCopy.WriteToServerAsync(dataTable, ct);

            await transaction.CommitAsync(ct);
            _logger.LogInformation("[{entityLogicalName}]: End BulkCopy", entityLogicalName);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);

            _logger.LogError("[{entityLogicalName}]: {ex}", entityLogicalName, ex);
            throw;
        }
    }

    public async Task CleanUpTableAsync(string entityLogicalName, CancellationToken ct)
    {
        var statement = _queryBuilderService.GetTruncateTableStatement(entityLogicalName);

        using var connection = _dbConnectionFactory.Create();
        await connection.OpenAsync(ct);

        var transaction = await connection.BeginTransactionAsync(ct);
        try
        {
            using var command = new SqlCommand(statement, connection, (SqlTransaction)transaction);
            _logger.LogInformation("[{entityLogicalName}]: Start CleanUpTable", entityLogicalName);
            await command.ExecuteNonQueryAsync(ct);

            await transaction.CommitAsync(ct);
            _logger.LogInformation("[{entityLogicalName}]: End CleanUpTable", entityLogicalName);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);

            _logger.LogError("[{entityLogicalName}]: {ex}", entityLogicalName, ex);
            throw;
        }
    }

    private void SetupBulkCopy(SqlBulkCopy sqlBulkCopy, string entityLogicalName, DataTable table)
    {
        sqlBulkCopy.BulkCopyTimeout = _fullSyncServiceOptions.TimeoutInSec;
        sqlBulkCopy.BatchSize = _fullSyncServiceOptions.BatchSize;
        sqlBulkCopy.DestinationTableName = entityLogicalName;
        foreach (DataColumn dataColumn in table.Columns)
        {
            sqlBulkCopy.ColumnMappings.Add(dataColumn.ColumnName, dataColumn.ColumnName);
        }
    }

    private DataTable GetDataTable(List<Dictionary<string, object>> records)
    {
        var columns = GetExistedColumns(records);

        var dataTable = new DataTable();
        foreach (var column in columns)
        {
            object value = GetAnyNotNullValue(records, column);
            if (value == null)
            {
                _logger.LogWarning("Dynamics returned null value or column \"{columnName}\" was not found", column);
                continue;
            }
            if (column.Equals(AzureSqlCommonAttributeNames.VersionNumber))
            {
                var dataType = value.GetType();
                dataTable.Columns.Add(column, dataType);
            }
            else
            {
                var type = value.GetType();
                dataTable.Columns.Add(column, type);
            }
        }

        foreach (var record in records)
        {
            var tableRow = dataTable.NewRow();
            foreach (DataColumn column in dataTable.Columns)
            {
                var value = record.TryGetValue(column.ColumnName, out var val) ? val : DBNull.Value;
                tableRow[column.ColumnName] = value is null ? DBNull.Value : value;
            }

            dataTable.Rows.Add(tableRow);
        }

        return dataTable;
    }

    private static object GetAnyNotNullValue(List<Dictionary<string, object>> records, string key)
    {
        foreach (var record in records)
        {
            if (record.TryGetValue(key, out object value) && value != null)
            {
                return value;
            }
        }
        return null;
    }

    private static List<string> GetExistedColumns(IEnumerable<Dictionary<string, object>> records)
    {
        return records.SelectMany(x => x.Keys).Distinct().OrderBy(x => x).ToList();
    }
}

