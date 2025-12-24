using RR.DataverseAzureSql.Common.Constants;
using RR.DataverseAzureSql.Services.Services.Sync;
using RR.DataverseAzureSql.Tests.Common.Factories;
using RR.DataverseAzureSql.Tests.Common.Mocks;
using Xunit;

namespace RR.DataverseAzureSql.Services.UnitTests.Services.Sync;

public class RealTimeSyncToAzureSqlServiceTests
{
    private readonly RealTimeSyncToAzureSqlService _realTimeSyncToAzureSqlService;
    private readonly FakeServiceBusMessageActions _messageActions;

    public RealTimeSyncToAzureSqlServiceTests()
    {
        _realTimeSyncToAzureSqlService = EntityFactory.CreateRealTimeSyncToAzureSqlService();
        _messageActions = EntityFactory.CreateFakeServiceBusMessageActions();
    }

    [Fact]
    public async Task Sync_ShouldProcess_MessageWithEmptyInputParameters_WithoutExceptions()
    {
        // Arrange
        var properties = new Dictionary<string, object>()
        {
            { "http://schemas.microsoft.com/xrm/2011/Claims/RequestName", "Create" }
        };
        var serviceBusMsg = EntityFactory.CreateFakeServiceBusReceivedMessage(
            Tests.Common.Properties.Resources.ServiceBusCreateMessageWithEmptyInputParameters, properties);

        // Act
        var exception = await Record.ExceptionAsync(async () => await _realTimeSyncToAzureSqlService.Sync(EntityLogicalNames.Annotation,
            new[] { serviceBusMsg }, _messageActions, default));

        // Assert
        Assert.Null(exception);
    }
}

