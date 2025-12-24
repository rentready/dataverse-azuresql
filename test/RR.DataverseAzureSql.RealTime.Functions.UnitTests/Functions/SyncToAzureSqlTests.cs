using System.Reflection;
using Azure.Messaging.ServiceBus;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using RR.DataverseAzureSql.RealTime.Functions.Functions;
using RR.DataverseAzureSql.Tests.Common.Factories;
using Xunit;
using RR.DataverseAzureSql.Common.Constants;

namespace RR.DataverseAzureSql.RealTime.Functions.UnitTests.Functions;

public class SyncToAzureSqlTests
{
    public static IEnumerable<object[]> ServiceBusTriggerFunctionMethods
    {
        get
        {
            return GetServiceBusTriggerFunctionMethods().Select(x => new object[] { x });
        }
    }

    [Fact]
    public void AllFunctions_ShouldHaveTheUniqueQueue()
    {
        // Arrange & Act
        var attributes = GetAllServiceBusTriggerAttributes().ToList();
        var groupedByQueueName = attributes.GroupBy(x => x.QueueName);

        // Assert
        foreach (var item in groupedByQueueName)
        {
            Assert.Single(item);
        }
    }

    [Theory]
    [MemberData(nameof(ServiceBusTriggerFunctionMethods))]
    public async Task AllServiceBusTriggers_ShouldPassMessages_ToRealTimeSyncToAzureSqlService(MethodInfo method)
    {
        // Arrange
        var serviceBusTriggerAttribute = method.GetParameters().First().GetCustomAttribute<ServiceBusTriggerAttribute>();
        Assert.NotNull(serviceBusTriggerAttribute);
        var realTimeSyncToAzureSqlService = EntityFactory.CreateFakeRealTimeSyncToAzureSqlService();
        var syncToAzureSql = EntityFactory.CreateSyncToAzureSql(realTimeSyncToAzureSqlService);
        long attemptCount = 1;
        var properties = new Dictionary<string, object>()
            {
                { ServiceBusMessageCustomPropertyNames.AttemptCount, attemptCount }
            };
        string messageBody = "{ \"id\": 1}";
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(new BinaryData(messageBody),
            properties: properties);
        var serviceBusMessageActions = EntityFactory.CreateFakeServiceBusMessageActions();
        // Act
        var task = method.Invoke(syncToAzureSql, new object[] { new ServiceBusReceivedMessage[] { message },
            serviceBusMessageActions, CancellationToken.None }) as Task;
        await task;
        // Assert
        var msgList = realTimeSyncToAzureSqlService.GetMessages();
        Assert.Single(msgList);
        var msg = msgList.Single();
        Assert.Equal(serviceBusTriggerAttribute.QueueName, msg.EntityLogicalName);
        Assert.Single(msg.Messages);

        Assert.Equal(messageBody, Encoding.UTF8.GetString(msg.Messages.Single().Body));
        Assert.Single(msg.Messages.Single().ApplicationProperties);
        Assert.Equal(attemptCount,
            (long)msg.Messages.Single().ApplicationProperties[ServiceBusMessageCustomPropertyNames.AttemptCount]);
    }

    private static IEnumerable<ServiceBusTriggerAttribute> GetAllServiceBusTriggerAttributes()
    {
        var functionType = typeof(SyncToAzureSql);
        foreach (var attribute in functionType.GetMethods()
            .Where(info => info.IsPublic && info.GetCustomAttribute<FunctionAttribute>() != null)
            .Select(x => x.GetParameters().First().GetCustomAttribute<ServiceBusTriggerAttribute>())
            .Where(x => x != null))
        {
            yield return attribute;
        }
    }

    private static IEnumerable<MethodInfo> GetServiceBusTriggerFunctionMethods()
    {
        var functionType = typeof(SyncToAzureSql);
        foreach (var methdoInfo in functionType.GetMethods()
            .Where(info => info.IsPublic && info.GetCustomAttribute<FunctionAttribute>() != null)
            .Where(x => x != null))
        {
            yield return methdoInfo;
        }
    }
}

