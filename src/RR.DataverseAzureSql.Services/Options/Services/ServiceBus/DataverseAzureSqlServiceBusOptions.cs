using System.ComponentModel.DataAnnotations;
using RR.Common.ServiceBus.Options;

namespace RR.DataverseAzureSql.Services.Options.Services.ServiceBus;

public class DataverseAzureSqlServiceBusOptions : ServiceBusClientOptionsBase
{
    [Required(ErrorMessage = "DefaultScheduledEnqueueTimeInMs is null or empty.")]
    public int DefaultScheduledEnqueueTimeInMs { get; set; }
}

