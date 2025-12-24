using RR.DataverseAzureSql.Common.Dtos;

namespace RR.DataverseAzureSql.Common.Interfaces.Services.AzureSql;

public interface IQueryBuilderService
{
    string GetInsertEntityStatement(string entityLogicalName, Dictionary<string, object> record);
    string GetUpdateEntityStatement(string entityLogicalName, Dictionary<string, object> record);
    string GetUpsertEntityStatement(string entityLogicalName, Dictionary<string, object> record);
    string GetDeleteEntityStatement(string entityLogicalName, Dictionary<string, object> record);
    string GetDeleteEntityStatement(string entityLogicalName, Guid id);
    string GetCreateOrUpdateTableSchemaStatement(string entityLogicalName, List<AzureSqlColumnDto> columns);
    string GetTruncateTableStatement(string entityLogicalName);
    string RemoveVersionNumberCondition(string statement);
}

