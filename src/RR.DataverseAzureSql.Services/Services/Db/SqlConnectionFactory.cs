using System.Data.SqlClient;
using Microsoft.Extensions.Options;
using RR.Common.Validations;
using RR.DataverseAzureSql.Common.Interfaces.Services.Db;
using RR.DataverseAzureSql.Services.Options.Services.AzureSql;

namespace RR.DataverseAzureSql.Services.Services.Db
{
    internal class SqlConnectionFactory : ISqlConnectionFactory
    {
        private readonly AzureSqlServiceOptions _options;

        public SqlConnectionFactory(IOptions<AzureSqlServiceOptions> options)
        {
            _options = options.Value.IsNotNull(nameof(options));
        }

        public SqlConnection Create()
        {
            return new SqlConnection(_options.ConnectionString);
        }
    }
}
