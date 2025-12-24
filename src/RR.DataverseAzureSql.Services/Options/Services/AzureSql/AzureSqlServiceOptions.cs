using System.ComponentModel.DataAnnotations;

namespace RR.DataverseAzureSql.Services.Options.Services.AzureSql;

public class AzureSqlServiceOptions
{
    [Required(ErrorMessage = "ConnectionString is null or empty.")]
    public string ConnectionString { get; set; }
}

