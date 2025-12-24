using System.ComponentModel.DataAnnotations;

namespace RR.DataverseAzureSql.Services.Options.Services.Storage;

public class TableServiceOptions
{
    [Required(ErrorMessage = "ConnectionString is null or empty.")]
    public string ConnectionString { get; set; }

    [Required(ErrorMessage = "TableName is null or empty.")]
    public string TableName { get; set; }
}

