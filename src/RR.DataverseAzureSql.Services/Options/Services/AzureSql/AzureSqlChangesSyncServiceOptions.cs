using System.ComponentModel.DataAnnotations;

namespace RR.DataverseAzureSql.Services.Options.Services.AzureSql;

public class AzureSqlChangesSyncServiceOptions
{
    [Required(ErrorMessage = "InsertBatchSize is null or empty.")]
    public int InsertBatchSize { get; set; }

    [Required(ErrorMessage = "InsertBatchSize is null or empty.")]
    public int UpdateBatchSize { get; set; }

    [Required(ErrorMessage = "InsertBatchSize is null or empty.")]
    public int DeleteBatchSize { get; set; }
}

