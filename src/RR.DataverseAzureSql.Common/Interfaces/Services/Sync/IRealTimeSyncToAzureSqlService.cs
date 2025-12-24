using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;

namespace RR.DataverseAzureSql.Common.Interfaces.Services.Sync;

public interface IRealTimeSyncToAzureSqlService
{
    Task Sync(string entityLogicalName, ServiceBusReceivedMessage[] messages, ServiceBusMessageActions messageActions, CancellationToken ct);
}

