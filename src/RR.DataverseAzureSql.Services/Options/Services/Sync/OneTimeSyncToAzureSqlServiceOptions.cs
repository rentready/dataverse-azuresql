using System.ComponentModel.DataAnnotations;

namespace RR.DataverseAzureSql.Services.Options.Services.Sync;

public class OneTimeSyncToAzureSqlServiceOptions
{
    [Required(ErrorMessage = "SynchronizedEntityLogicalNames is null or empty.")]
    public string SynchronizedEntityLogicalNames { get; set; }

    [Required(ErrorMessage = "BatchSize is null or empty.")]
    public int BatchSize { get; set; }

    [Required(ErrorMessage = "MaxActivityRetryCount is null or empty.")]
    public int MaxActivityRetryCount { get; set; }

    [Required(ErrorMessage = "ActivityRetryIntervalInSec is null or empty.")]
    public int ActivityRetryIntervalInSec { get; set; }
}

