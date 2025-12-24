using System.Collections.ObjectModel;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using RR.DataverseAzureSql.Common.Interfaces.Services.Sync;

namespace RR.DataverseAzureSql.Tests.Common.Mocks.Services.AzureSql
{
    public class FakeRealTimeSyncToAzureSqlService : IRealTimeSyncToAzureSqlService
    {
        private readonly List<RealTimeSyncToAzureSqlDto> _msgList = new List<RealTimeSyncToAzureSqlDto>();

        public Task Sync(string entityLogicalName, ServiceBusReceivedMessage[] messages, ServiceBusMessageActions messageActions, CancellationToken ct)
        {
            _msgList.Add(new RealTimeSyncToAzureSqlDto(entityLogicalName, messages));
            return Task.CompletedTask;
        }

        public ReadOnlyCollection<RealTimeSyncToAzureSqlDto> GetMessages()
        {
            return _msgList.AsReadOnly();
        }
    }

    public class RealTimeSyncToAzureSqlDto
    {
        public RealTimeSyncToAzureSqlDto(string entityLogicalName, ServiceBusReceivedMessage[] messages)
        {
            EntityLogicalName = entityLogicalName;
            Messages = messages;
        }

        public string EntityLogicalName { get; }
        public ServiceBusReceivedMessage[] Messages { get; }
    }
}
