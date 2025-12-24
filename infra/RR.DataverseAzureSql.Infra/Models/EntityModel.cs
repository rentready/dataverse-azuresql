namespace RR.DataverseAzureSql.Infra.Models;

public class EntityModel
{
    public string Entity { get; set; }
    public string Id { get; set; }
    public List<MessageModel> Messages { get; set; }
}

