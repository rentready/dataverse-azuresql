using Azure.Messaging.ServiceBus;

namespace RR.DataverseAzureSql.Common.Interfaces.Services.ServiceBus;

public interface IServiceBusService
{
    Task SendMessageAsync(string queueName, ServiceBusReceivedMessage originalMessage, CancellationToken ct);
    Task SendMessagesAsync(string queueName, List<ServiceBusReceivedMessage> originalMessages, CancellationToken ct);
}

