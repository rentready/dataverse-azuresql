using Microsoft.Xrm.Sdk.Messages;

namespace RR.DataverseAzureSql.Common.Extensions;

public static class DisassociateRequestExtensions
{
    public static List<Dictionary<string, object>> ToDictionaryCollection(this DisassociateRequest request)
    {
        List<Dictionary<string, object>> documents = new();

        foreach (var entityReference in request.RelatedEntities.Select(x => x))
        {
            var document = new Dictionary<string, object>
            {
                { $"{request.Target.LogicalName}id", request.Target.Id },
                { $"{entityReference.LogicalName}id", entityReference.Id }
            };
            documents.Add(document);
        }

        return documents;
    }
}

