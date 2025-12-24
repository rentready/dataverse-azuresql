using Microsoft.Xrm.Sdk;
using RR.Common.Interfaces;
using RR.Common.Validations;
using RR.DataverseAzureSql.Common.Constants;
using RR.DataverseAzureSql.Common.Interfaces.Services.Configs;
using RR.DataverseAzureSql.Common.Interfaces.Services.Converters;

namespace RR.DataverseAzureSql.Services.Services.Converters;

public class EntityConverter : IEntityConverter
{
    private readonly IDateTimeConverter _dateTimeConverter;
    private readonly ICurrentTimeService _currentTimeService;
    private readonly IRetrieveEntitySettingsConfigProvider _retrieveEntitySettingsConfigProvider;

    public EntityConverter(IDateTimeConverter dateTimeConverter,
        ICurrentTimeService currentTimeService,
        IRetrieveEntitySettingsConfigProvider retrieveEntitySettingsConfigProvider)
    {
        _dateTimeConverter = dateTimeConverter.IsNotNull(nameof(dateTimeConverter));
        _currentTimeService = currentTimeService.IsNotNull(nameof(currentTimeService));
        _retrieveEntitySettingsConfigProvider = retrieveEntitySettingsConfigProvider.IsNotNull(nameof(retrieveEntitySettingsConfigProvider));
    }

    public Dictionary<string, object> ToDictionary(Entity entity)
    {
        Dictionary<string, object> document = new()
        {
            { AzureSqlCommonAttributeNames.Id, entity.Id },
        };
        var exludedAtrbs = new HashSet<string> { DynamicsCommonAttributeNames.CreatedOn, DynamicsCommonAttributeNames.ModifiedOn };
        foreach (var attribute in entity.Attributes.Where(x => !exludedAtrbs.Contains(x.Key)))
        {
            switch (attribute.Value)
            {
                case EntityCollection:
                    break;
                case EntityReference value:
                    document.Add(attribute.Key, value.Id);
                    document.Add($"{attribute.Key}_entitytype", value.LogicalName);
                    break;
                case OptionSetValue value:
                    document.Add(attribute.Key, value.Value);
                    break;
                case OptionSetValueCollection value:
                    document.Add(attribute.Key, $"{string.Join(";", value.Select(x => x.Value))}");
                    break;
                case Money value:
                    document.Add(attribute.Key, value.Value);
                    break;
                case Boolean:
                    document.Add(attribute.Key, attribute.Value);
                    break;
                case String value:
                    document.Add(attribute.Key, ToDateTime(value));
                    break;
                case Guid value:
                    document.Add(attribute.Key, value);
                    break;
                case DateTime value:
                    document.Add(attribute.Key, value);
                    break;
                case Int32:
                    document.Add(attribute.Key, attribute.Value);
                    break;
                case Int64:
                    document.Add(attribute.Key, attribute.Value);
                    break;
                case Decimal value:
                    document.Add(attribute.Key, value);
                    break;
                case Double value:
                    document.Add(attribute.Key, value);
                    break;
                case Object[] values:
                    if (values[0] is OptionSetValue)
                    {
                        List<long> output = new();

                        foreach (OptionSetValue value in values)
                        {
                            output.Add(value.Value);
                        }

                        document.Add(attribute.Key, $"{string.Join(";", output.Select(x => x))}");
                    }
                    break;
                case Object value:
                    document.Add(attribute.Key, value);
                    break;
                default:
                    document.Add(attribute.Key, attribute.Value);
                    break;

            }
        }

        AddRequiredFields(entity.Attributes.Where(x => exludedAtrbs.Contains(x.Key)), document);
        AddVersionNumberIfNeeded(document, entity);
        ExcludeFieldsSpecifiedInConfig(document, entity.LogicalName);
        return document;
    }

    internal void ExcludeFieldsSpecifiedInConfig(Dictionary<string, object> document, string entityLogicalName)
    {
        var settings = _retrieveEntitySettingsConfigProvider.Get();
        if (settings == null)
        {
            return;
        }
        var setting = Array.Find(settings, x => x.EntityLogicalName == entityLogicalName);
        if (setting == null)
        {
            return;
        }
        foreach (var excludedField in setting.ExcludedFields.Where(x => document.ContainsKey(x)))
        {
            document.Remove(excludedField);
        }
    }

    internal void AddRequiredFields(IEnumerable<KeyValuePair<string, object>> atrbList, Dictionary<string, object> document)
    {
        void SetDocument(string dynamicsAtrbName, string documentAtrbName)
        {
            var atrb = atrbList.FirstOrDefault(x => x.Key == dynamicsAtrbName);
            if (atrb.Equals(default(KeyValuePair<string, object>)))
            {
                document.Add(documentAtrbName, _currentTimeService.GetCurrentUTCTime());
            }
            else
            {
                switch (atrb.Value)
                {
                    case String value:
                        document.Add(atrb.Key, ToDateTime(value));
                        document.Add(documentAtrbName, ToDateTime(value));
                        break;
                    case DateTime value:
                        document.Add(atrb.Key, value);
                        document.Add(documentAtrbName, value);
                        break;
                    default:
                        break;
                }
            }
        }
        SetDocument(DynamicsCommonAttributeNames.CreatedOn, AzureSqlCommonAttributeNames.SinkCreatedOn);
        SetDocument(DynamicsCommonAttributeNames.ModifiedOn, AzureSqlCommonAttributeNames.SinkModifiedOn);
    }

    private static void AddVersionNumberIfNeeded(Dictionary<string, object> document, Entity entity)
    {
        if (!document.ContainsKey(AzureSqlCommonAttributeNames.VersionNumber))
        {
            document.Add(AzureSqlCommonAttributeNames.VersionNumber,
                long.TryParse(entity.RowVersion, out long versionNumber) ? versionNumber : (long?)null);
        }
    }

    private object ToDateTime(string data)
    {
        var dateTime = _dateTimeConverter.UnixEpochDateTimeConverter(data);

        return dateTime is null ? data : dateTime;
    }
}

