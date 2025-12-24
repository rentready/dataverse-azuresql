using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RR.Common.General;
using RR.Common.Interfaces;
using RR.Common.ServiceBus.Extensions;
using RR.Common.Validations;
using RR.DataverseAzureSql.Common.Constants;
using RR.DataverseAzureSql.Common.Interfaces.Services.ServiceBus;
using RR.DataverseAzureSql.Services.Options.Services.ServiceBus;

namespace RR.DataverseAzureSql.Services.Services.ServiceBus;

public class ServiceBusService : IServiceBusService
{
    private readonly IAzureClientFactory<ServiceBusClient> _azureClientFactory;
    private readonly ICurrentTimeService _currentTimeService;
    private readonly ILogger<ServiceBusService> _logger;
    private readonly DataverseAzureSqlServiceBusOptions _options;
    private readonly LazyWithoutExceptionCaching<ServiceBusClient> _lazyClient;

    private ServiceBusClient ServiceBusClient => _lazyClient.Value;

    public ServiceBusService(IAzureClientFactory<ServiceBusClient> azureClientFactory,
        ICurrentTimeService currentTimeService,
        IOptions<DataverseAzureSqlServiceBusOptions> options,
        ILogger<ServiceBusService> logger)
    {
        _azureClientFactory = azureClientFactory.IsNotNull(nameof(azureClientFactory));
        _currentTimeService = currentTimeService.IsNotNull(nameof(currentTimeService));
        _logger = logger.IsNotNull(nameof(logger));
        _options = options.Value;

        _lazyClient = new LazyWithoutExceptionCaching<ServiceBusClient>(CreateServiceBusClient);
    }

    public async Task SendMessageAsync(string queueName,
        ServiceBusReceivedMessage originalMessage, CancellationToken ct)
    {
        var message = CreateScheduledMessage(originalMessage);

        var scheduledEnqueueTime = _currentTimeService.GetCurrentUTCTime()
            .AddMilliseconds(_options.DefaultScheduledEnqueueTimeInMs);

        await using var sender = ServiceBusClient.CreateSender(queueName);
        await sender.ScheduleMessageAsync(message, scheduledEnqueueTime, ct);
    }

    public async Task SendMessagesAsync(string queueName,
        List<ServiceBusReceivedMessage> originalMessages, CancellationToken ct)
    {
        var messages = originalMessages.Select(originalMessage =>
            CreateScheduledMessage(originalMessage));

        var scheduledEnqueueTime = _currentTimeService.GetCurrentUTCTime()
            .AddMilliseconds(_options.DefaultScheduledEnqueueTimeInMs);

        await using var sender = ServiceBusClient.CreateSender(queueName);
        await sender.ScheduleMessagesAsync(messages, scheduledEnqueueTime, ct);
    }

    private ServiceBusMessage CreateScheduledMessage(ServiceBusReceivedMessage originalMessage)
    {
        var attemptCountPropertyName = ServiceBusMessageCustomPropertyNames.AttemptCount;
        var message = new ServiceBusMessage(originalMessage.Body);

        long attempt = 0;
        if (originalMessage.ApplicationProperties.TryGetValue(attemptCountPropertyName, out var value))
        {
            if (value is long longValue)
            {
                attempt = longValue;
            }
            else
            {
                _logger.LogWarning("{attemptCountPropertyName} is not long , the type is {type}",
                    attemptCountPropertyName, value.GetType());
            }
        }
        attempt += 1;

        message.ApplicationProperties.Add(attemptCountPropertyName, attempt);

        foreach (var property in originalMessage.ApplicationProperties
            .Where(x => !x.Key.Equals(attemptCountPropertyName, StringComparison.OrdinalIgnoreCase)))
        {
            message.ApplicationProperties.Add(property.Key, property.Value);
        }

        return message;
    }

    private ServiceBusClient CreateServiceBusClient()
    {
        return _azureClientFactory.CreateClient<DataverseAzureSqlServiceBusOptions>();
    }
}

