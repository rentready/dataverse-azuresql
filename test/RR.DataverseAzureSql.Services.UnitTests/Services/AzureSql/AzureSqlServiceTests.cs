using RR.DataverseAzureSql.Common.Constants;
using RR.DataverseAzureSql.Common.Exceptions;
using RR.DataverseAzureSql.Tests.Common.Factories;
using Xunit;

namespace RR.DataverseAzureSql.Services.UnitTests.Services.AzureSql
{
    public class AzureSqlServiceTests
    {
        [Fact]
        public async Task Delete_ShouldThrowTableNotExistException_ById()
        {
            // Arrange
            using var connectionFactory = EntityFactory.CreateSqlExpressDbConnectionFactory();
            var service = EntityFactory.CreateAzureSqlService(connectionFactory);
            // Act
            var exception = await Assert.ThrowsAsync<TableNotExistException>(() =>
                service.DeleteAsync(EntityLogicalNames.WorkOrder, Guid.NewGuid(), default));
            // Assert
            Assert.NotNull(exception);
        }

        [Fact]
        public async Task Delete_ShouldThrowTableNotExistException_ByManyColumns()
        {
            // Arrange
            using var connectionFactory = EntityFactory.CreateSqlExpressDbConnectionFactory();
            var service = EntityFactory.CreateAzureSqlService(connectionFactory);
            // Act
            var exception = await Assert.ThrowsAsync<TableNotExistException>(() =>
                service.DeleteAsync(EntityLogicalNames.WorkOrder, new Dictionary<string, object>
                {
                    { "id1", Guid.NewGuid() },
                    { "id2", Guid.NewGuid() },
                }, default));
            // Assert
            Assert.NotNull(exception);
        }
    }
}
