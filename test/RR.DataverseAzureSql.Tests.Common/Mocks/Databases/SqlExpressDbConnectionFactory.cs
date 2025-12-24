using System.Data.SqlClient;
using System.Runtime.CompilerServices;
using RR.Common.General;
using RR.DataverseAzureSql.Common.Interfaces.Services.Db;

namespace RR.DataverseAzureSql.Tests.Common.Mocks.Databases
{
    public sealed class SqlExpressDbConnectionFactory : ISqlConnectionFactory, IDisposable
    {
        private static int _dbGlobalNumber = 0;
        private readonly int _dbNumber;
        private readonly string _masterConnectionString;
        private readonly string _connectionString;
        private readonly LazyWithoutExceptionCaching<Task> _lazyCreateDatabase;
        private bool _disposed;
        private readonly string _caller;

        public SqlExpressDbConnectionFactory([CallerMemberName] string caller = null)
        {
            _caller = caller;
            _dbNumber = Interlocked.Increment(ref _dbGlobalNumber);
            _masterConnectionString = $"Server=localhost;Database=master;User Id=sa;Password=6xA3mU3jXrlWqK0X;Trusted_Connection=False;";
            _connectionString = $"Server=localhost;Database={GetDatabaseName()};User Id=sa;Password=6xA3mU3jXrlWqK0X;Trusted_Connection=False;";
            _lazyCreateDatabase = new LazyWithoutExceptionCaching<Task>(CreateDatabase);
        }

        public SqlConnection Create()
        {
            _lazyCreateDatabase.Value.ConfigureAwait(false).GetAwaiter().GetResult();
            var connection = new SqlConnection(_connectionString);
            return connection;
        }

        public string GetConnectionString()
        {
            return _connectionString;
        }

        private string GetDatabaseName()
        {
            string name = $"{_caller}DynamicsReplica{_dbNumber}";
            return name;
        }

        private async Task CreateDatabase()
        {
            using var connection = new SqlConnection(_masterConnectionString);
            await connection.OpenAsync();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"CREATE DATABASE {GetDatabaseName()}";
            await cmd.ExecuteNonQueryAsync();
            connection.Close();
        }

        private void DropDatabase()
        {
            using var connection = new SqlConnection(_masterConnectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"IF DB_ID('{GetDatabaseName()}') IS NOT NULL Begin ALTER DATABASE [{GetDatabaseName()}] set single_user with rollback immediate " +
                $"DROP DATABASE IF EXISTS {GetDatabaseName()} End";
            cmd.ExecuteNonQuery();
            connection.Close();
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                DropDatabase();
            }
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
