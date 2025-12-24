using System.ComponentModel.DataAnnotations;

namespace RR.DataverseAzureSql.Services.Options.Services.AzureSql;

public class AzureSqlFullSyncServiceOptions
{
    [Required(ErrorMessage = "BatchSize is null or empty.")]
    public int BatchSize { get; set; }

    [Required(ErrorMessage = "TimeoutInSec is null or empty.")]
    public int TimeoutInSec { get; set; }
}

