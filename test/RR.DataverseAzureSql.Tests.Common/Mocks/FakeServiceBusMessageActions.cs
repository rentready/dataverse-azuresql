using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using RR.DataverseAzureSql.Tests.Common.Properties;

namespace RR.DataverseAzureSql.Tests.Common.Mocks;

public class FakeServiceBusMessageActions : ServiceBusMessageActions
{
    public readonly List<ServiceBusReceivedMessage> CompletedMessages = new List<ServiceBusReceivedMessage>();

    public override Task DeadLetterMessageAsync(ServiceBusReceivedMessage message, Dictionary<string, object> propertiesToModify = null, string deadLetterReason = null, string deadLetterErrorDescription = null, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public override Task CompleteMessageAsync(ServiceBusReceivedMessage message, CancellationToken cancellationToken = default)
    {
        CompletedMessages.Add(message);
        return Task.CompletedTask;
    }
}

