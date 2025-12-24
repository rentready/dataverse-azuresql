namespace RR.DataverseAzureSql.Common.Dtos;

public class AzureSqlColumnDto
{
    public string Name { get; set; }
    public string DataType { get; set; }
    public int? DataLength { get; set; }
    public int? DataPresition { get; set; }
    public bool? IsNullable { get; set; }
}

