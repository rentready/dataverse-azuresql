using System.Runtime.Serialization.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Xrm.Sdk;
using RR.DataverseAzureSql.Common.Constants;
using RR.DataverseAzureSql.Common.Enums;

namespace RR.DataverseAzureSql.Common.Extensions;

public static class ServiceBusReceivedMessageExtensions
{
    public static DynamicsRequestType GetRequestType(this ServiceBusReceivedMessage message)
    {
        var requestType = message.ApplicationProperties
            .TryGetValue("http://schemas.microsoft.com/xrm/2011/Claims/RequestName", out var result)
            ? result.ToString()
            : throw new KeyNotFoundException($"Reason: Could not find RequestName in ApplicationProperties, SequenceNumber: {message.SequenceNumber}, ApplicationProperties: {string.Join(",", message.ApplicationProperties.Select(x => x.Key + ": " + x.Value.ToString()))}");

        return requestType switch
        {
            "Create" => DynamicsRequestType.Create,
            "Update" => DynamicsRequestType.Update,
            "Delete" => DynamicsRequestType.Delete,
            "Associate" => DynamicsRequestType.Associate,
            "Disassociate" => DynamicsRequestType.Disassociate,
            _ => throw new ArgumentOutOfRangeException($"Unknown dynamics request type '{requestType}' into message."),
        };
    }

    public static string GetEntityLogicalName(this ServiceBusReceivedMessage message)
    {
        var entityName = message.ApplicationProperties
            .TryGetValue("http://schemas.microsoft.com/xrm/2011/Claims/EntityLogicalName", out var result)
            ? result.ToString()
            : throw new KeyNotFoundException($"Reason: Could not find EntityLogicalName in ApplicationProperties, SequenceNumber: {message.SequenceNumber}, ApplicationProperties: {string.Join(",", message.ApplicationProperties.Select(x => x.Key + ": " + x.Value.ToString()))}");

        return entityName;
    }

    public static int GetAttemptCount(this ServiceBusReceivedMessage message)
    {
        var attemptCount = message.ApplicationProperties
            .TryGetValue(ServiceBusMessageCustomPropertyNames.AttemptCount, out var result)
            ? result.ToString()
            : null;

        return string.IsNullOrEmpty(attemptCount)
            ? 0
            : int.Parse(attemptCount);
    }

    public static RemoteExecutionContext ToRemoteExecutionContext(this ServiceBusReceivedMessage message)
    {
        return (RemoteExecutionContext)
            new DataContractJsonSerializer(typeof(RemoteExecutionContext))
                .ReadObject(new MemoryStream(message.Body.ToArray()));
    }
}

