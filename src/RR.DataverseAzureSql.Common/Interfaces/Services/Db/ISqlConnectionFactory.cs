using System.Data.SqlClient;

namespace RR.DataverseAzureSql.Common.Interfaces.Services.Db
{
    public interface ISqlConnectionFactory
    {
        public SqlConnection Create();
    }
}
