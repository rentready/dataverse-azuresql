using System.Data.SqlClient;
using Microsoft.Extensions.Logging;
using RR.Common.Validations;
using RR.DataverseAzureSql.Common.Dtos;
using RR.DataverseAzureSql.Common.Interfaces.Services.AzureSql;
using RR.DataverseAzureSql.Common.Interfaces.Services.Db;
using RR.DataverseAzureSql.Common.Interfaces.Services.Metadata;

namespace RR.DataverseAzureSql.Services.Services.AzureSql
{
    public class SqlSchemaService : ISqlSchemaService
    {
        private readonly IDynamicsEntityAttributesMetadataService _dynamicsMetadataService;
        private readonly IQueryBuilderService _queryBuilderService;
        private readonly ISqlConnectionFactory _sqlConnectionFactory;
        private readonly ILogger<SqlSchemaService> _logger;
        private readonly IAzureSqlEntityAttributesMetadataService _azureSqlEntityAttributesMetadataService;

        public SqlSchemaService(IDynamicsEntityAttributesMetadataService dynamicsMetadataService,
            IQueryBuilderService queryBuilderService, ISqlConnectionFactory sqlConnectionFactory,
            IAzureSqlEntityAttributesMetadataService azureSqlEntityAttributesMetadataService,
            ILogger<SqlSchemaService> logger)
        {
            _dynamicsMetadataService = dynamicsMetadataService.IsNotNull(nameof(dynamicsMetadataService));
            _queryBuilderService = queryBuilderService.IsNotNull(nameof(queryBuilderService));
            _sqlConnectionFactory = sqlConnectionFactory.IsNotNull(nameof(sqlConnectionFactory));
            _logger = logger.IsNotNull(nameof(logger));
            _azureSqlEntityAttributesMetadataService = azureSqlEntityAttributesMetadataService;
        }

        public async Task CreateOrUpdateTableSchema(string tableName, CancellationToken ct)
        {
            var attributes = (await _dynamicsMetadataService.GetAsync(tableName)).Attributes;
            var columns = _azureSqlEntityAttributesMetadataService.Get(attributes);
            await CreateOrUpdateTableSchemaAsync(tableName, columns, ct);
        }

        private async Task CreateOrUpdateTableSchemaAsync(string tableName, List<AzureSqlColumnDto> columns, CancellationToken ct)
        {
            var statement = _queryBuilderService.GetCreateOrUpdateTableSchemaStatement(tableName, columns);

            using var connection = _sqlConnectionFactory.Create();
            await connection.OpenAsync(ct);

            var transaction = await connection.BeginTransactionAsync(ct);
            try
            {
                _logger.LogInformation("[{entityLogicalName}]: Start CreateOrUpdateTableSchema", tableName);
                using var command = new SqlCommand(statement, connection, (SqlTransaction)transaction);
                await command.ExecuteNonQueryAsync(ct);

                await transaction.CommitAsync(ct);
                _logger.LogInformation("[{entityLogicalName}]: End CreateOrUpdateTableSchema", tableName);
            }
            catch (SqlException ex)
            {
                await transaction.RollbackAsync(ct);

                _logger.LogError("[{entityLogicalName}]: Type: {number} Exception: {ex}", tableName, ex.Number, ex);
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);

                _logger.LogError("[{entityLogicalName}]: {ex}", tableName, ex);
                throw;
            }
        }
    }
}
