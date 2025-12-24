using Microsoft.Extensions.Logging;
using RR.Common.Validations;
using RR.DataverseAzureSql.Common.Constants;
using RR.DataverseAzureSql.Common.Dtos;
using RR.DataverseAzureSql.Common.Interfaces.Services.AzureSql;

namespace RR.DataverseAzureSql.Services.Services.AzureSql;

public class QueryBuilderService : IQueryBuilderService
{
    private readonly ILogger<QueryBuilderService> _logger;

    private readonly string VersionNumberCondition = "AND tgt.versionnumber <= src.versionnumber";

    public QueryBuilderService(ILogger<QueryBuilderService> logger)
    {
        _logger = logger.IsNotNull(nameof(logger));
    }

    public string GetInsertEntityStatement(string entityLogicalName, Dictionary<string, object> record)
    {
        var values = record.Values.Select(ToString);
        var statement =
            $"INSERT INTO {entityLogicalName} ({string.Join(",", record.Keys)}) " +
            $"VALUES ({string.Join(",", values)});";

        _logger.LogTrace("Command: {statement}", statement);

        return statement;
    }

    public string GetUpdateEntityStatement(string entityLogicalName, Dictionary<string, object> record)
    {
        var columns = record.Select(x => $"{x.Key}={ToString(x.Value)}");
        var statement =
            $"UPDATE TOP (1) {entityLogicalName} " +
            $"SET {string.Join(",", columns)} " +
            $"WHERE {AzureSqlCommonAttributeNames.Id} = '{record[AzureSqlCommonAttributeNames.Id]}';";

        _logger.LogTrace("Command: {statement}", statement);

        return statement;
    }

    public string GetUpsertEntityStatement(string entityLogicalName, Dictionary<string, object> record)
    {
        var columns = record.Select(x => $"tgt.{x.Key}=src.{x.Key}");
        var values = record.Values.Select(ToString);
        var statement =
            $"MERGE INTO {entityLogicalName} AS tgt " +
            $"USING (VALUES({string.Join(",", values)})) AS src ({string.Join(",", record.Keys)}) " +
            $"ON tgt.{AzureSqlCommonAttributeNames.Id} = src.{AzureSqlCommonAttributeNames.Id} " +
            $"WHEN MATCHED {IsVersionNumberEqualOrLess(record[AzureSqlCommonAttributeNames.VersionNumber])} THEN " +
            $"UPDATE SET {string.Join(", ", columns)} " +
            $"WHEN NOT MATCHED BY TARGET THEN " +
            $"INSERT ({string.Join(",", record.Keys)}) VALUES ({string.Join(",", values)});";

        _logger.LogTrace("Command: {statement}", statement);

        return statement;
    }

    public string GetDeleteEntityStatement(string entityLogicalName, Dictionary<string, object> record)
    {
        var columns = record.Select(x => $"{x.Key}={ToString(x.Value)}");
        var statement =
            $"DELETE FROM {entityLogicalName} " +
            $"WHERE {string.Join(" AND ", columns)};";

        _logger.LogTrace("Command: {statement}", statement);

        return statement;
    }

    public string GetDeleteEntityStatement(string entityLogicalName, Guid id)
    {
        var statement =
            $"DELETE FROM {entityLogicalName} " +
            $"WHERE {AzureSqlCommonAttributeNames.Id} = '{id}';";

        _logger.LogTrace("Command: {statement}", statement);

        return statement;
    }

    public string GetCreateOrUpdateTableSchemaStatement(string entityLogicalName, List<AzureSqlColumnDto> columns)
    {
        var tableSchema = "dbo";
        var newTableColumns = new List<string>();
        var newAddedColumns = new List<string>();

        columns.ForEach(x =>
        {
            string dataType;

            var isNullable = x.IsNullable ?? true ? "NULL" : "NOT NULL";

            if (!x.DataPresition.HasValue && !x.DataLength.HasValue)
                dataType = x.DataType;
            else if (!x.DataPresition.HasValue && x.DataLength.HasValue)
                dataType = x.DataLength == -1 ? $"{x.DataType}(MAX)" : $"{x.DataType}({x.DataLength})";
            else
                dataType = $"{x.DataType}({x.DataLength},{x.DataPresition})";

            newTableColumns.Add($"[{x.Name}] {dataType} {isNullable}");
            newAddedColumns.Add(
                $"IF COL_LENGTH(N'{tableSchema}.{entityLogicalName}', '{x.Name}') IS NULL " +
                $"ALTER TABLE {tableSchema}.{entityLogicalName} " +
                $"ADD [{x.Name}] {dataType} {isNullable}"
            );
        });

        newTableColumns.Add($"CONSTRAINT [EPK[{tableSchema}]].[{entityLogicalName}]]] PRIMARY KEY CLUSTERED(Id)");

        var statement =
            $"IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'{tableSchema}.{entityLogicalName}') AND type in (N'U'))" +
            $"CREATE TABLE {tableSchema}.{entityLogicalName} ({string.Join(",", newTableColumns)})" +
            $"ELSE " +
            $"{string.Join(";", newAddedColumns)}";

        _logger.LogTrace("Command: {statement}", statement);

        return statement;
    }

    public string GetTruncateTableStatement(string entityLogicalName)
    {
        var tableSchema = "dbo";

        var statement =
            $"IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'{tableSchema}.{entityLogicalName}') AND type in (N'U'))" +
            $"TRUNCATE TABLE {tableSchema}.{entityLogicalName}";

        _logger.LogTrace("Command: {statement}", statement);

        return statement;
    }

    public string RemoveVersionNumberCondition(string statement)
        => statement.Replace(VersionNumberCondition, "");

    private string IsVersionNumberEqualOrLess(object versionNumber)
        => versionNumber is null ? "" : VersionNumberCondition;

    private static string ToString(object data)
        => data is null ? "null" : string.Format("'{0}'", EscapeSingleQuote(data));

    private static string EscapeSingleQuote(object data)
        => data.ToString().Replace("'", "''");
}

