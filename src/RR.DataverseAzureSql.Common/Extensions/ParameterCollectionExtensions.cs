using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using RR.DataverseAzureSql.Common.Constants;

namespace RR.DataverseAzureSql.Common.Extensions;

public static class ParameterCollectionExtensions
{
    public static Entity ToEntity(this ParameterCollection parameters)
    {
        return (Entity)parameters.FirstOrDefault().Value;
    }

    public static EntityReference ToEntityReference(this ParameterCollection parameters)
    {
        return (EntityReference)parameters.FirstOrDefault().Value;
    }

    public static AssociateRequest ToAssociateRequest(this ParameterCollection parameters)
    {
        var (target, relationship, relatedEntities) = GetRequest(parameters);
        return new AssociateRequest
        {
            Target = target,
            Relationship = relationship,
            RelatedEntities = new EntityReferenceCollection(relatedEntities)
        };
    }

    public static DisassociateRequest ToDisassociateRequest(this ParameterCollection parameters)
    {
        var (target, relationship, relatedEntities) = GetRequest(parameters);
        return new DisassociateRequest
        {
            Target = target,
            Relationship = relationship,
            RelatedEntities = new EntityReferenceCollection(relatedEntities)
        };
    }

    public static string GetChangedEntityLogicalName(this ParameterCollection parameters)
    {
        var changedEntity = parameters
            .TryGetValue(ParameterCollectionPropertyNames.ChangedEntityTypes, out object[] result)
            ? (KeyValuePair<string, string>)result[0]
            : new KeyValuePair<string, string>();

        return changedEntity.Key;
    }

    private static (EntityReference, Relationship, List<EntityReference>) GetRequest(ParameterCollection parameters)
    {
        var relationship = parameters
            .Where(x => x.Key.Equals(ParameterCollectionPropertyNames.Relationship))
            .Select(x => (Relationship)x.Value)
            .First();

        var target = parameters
            .Where(x => x.Key.Equals(ParameterCollectionPropertyNames.Target))
            .Select(x => (EntityReference)x.Value)
            .First();

        var relatedEntities = parameters
            .Where(x => x.Key.Equals(ParameterCollectionPropertyNames.RelatedEntities))
            .SelectMany(x => (object[])x.Value)
            .Select(x => (EntityReference)x)
            .ToList();

        return (target, relationship, relatedEntities);
    }
}

