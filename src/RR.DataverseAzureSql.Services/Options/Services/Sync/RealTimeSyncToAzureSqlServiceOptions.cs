using System.ComponentModel.DataAnnotations;

namespace RR.DataverseAzureSql.Services.Options.Services.Sync;

public class RealTimeSyncToAzureSqlServiceOptions
{
    [Required(ErrorMessage = "MaxMessageRetryCount is null or empty.")]
    public int MaxMessageRetryCount { get; set; }

    [Required(ErrorMessage = "RelationshipEntityLogicalNames is null or empty.")]
    public string RelationshipEntityLogicalNames { get; set; }
}

