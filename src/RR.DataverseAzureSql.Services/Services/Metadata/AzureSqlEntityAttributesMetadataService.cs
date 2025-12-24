using Microsoft.Xrm.Sdk.Metadata;
using System.Data;
using RR.DataverseAzureSql.Common.Interfaces.Services.Metadata;
using RR.DataverseAzureSql.Common.Dtos;
using Microsoft.Extensions.Logging;
using RR.Common.Validations;
using RR.DataverseAzureSql.Common.Constants;

namespace RR.DataverseAzureSql.Services.Services.Metadata;

public class AzureSqlEntityAttributesMetadataService : IAzureSqlEntityAttributesMetadataService
{
    private readonly ILogger<AzureSqlEntityAttributesMetadataService> _logger;

    public AzureSqlEntityAttributesMetadataService(ILogger<AzureSqlEntityAttributesMetadataService> logger)
    {
        _logger = logger.IsNotNull(nameof(logger));
    }

    public List<AzureSqlColumnDto> Get(IEnumerable<AttributeMetadata> attributes)
    {
        var columns = new List<AzureSqlColumnDto>
        {
            new AzureSqlColumnDto
            {
                Name = AzureSqlCommonAttributeNames.Id,
                DataType = Enum.GetName(typeof(SqlDbType), SqlDbType.UniqueIdentifier),
                IsNullable = false
            },

            new AzureSqlColumnDto
            {
                Name = AzureSqlCommonAttributeNames.SinkCreatedOn,
                DataType = Enum.GetName(typeof(SqlDbType), SqlDbType.DateTime)
            },

            new AzureSqlColumnDto
            {
                Name = AzureSqlCommonAttributeNames.SinkModifiedOn,
                DataType = Enum.GetName(typeof(SqlDbType), SqlDbType.DateTime)
            }
        };

        foreach (var attribute in attributes)
        {
            switch (attribute.AttributeType)
            {
                case AttributeTypeCode.Virtual:
                    if (attribute.AttributeTypeName.Value.Equals("MultiSelectPicklistType"))
                        columns.Add(new AzureSqlColumnDto
                        {
                            Name = attribute.LogicalName,
                            DataType = Enum.GetName(typeof(SqlDbType), SqlDbType.NVarChar),
                            DataLength = 4000
                        });
                    break;
                case AttributeTypeCode.CalendarRules:
                    break;
                case AttributeTypeCode.Uniqueidentifier:
                case AttributeTypeCode.PartyList:
                    columns.Add(new AzureSqlColumnDto
                    {
                        Name = attribute.LogicalName,
                        DataType = Enum.GetName(typeof(SqlDbType), SqlDbType.UniqueIdentifier)
                    });
                    break;
                case AttributeTypeCode.Lookup:
                case AttributeTypeCode.Customer:
                case AttributeTypeCode.Owner:
                    columns.Add(new AzureSqlColumnDto
                    {
                        Name = attribute.LogicalName,
                        DataType = Enum.GetName(typeof(SqlDbType), SqlDbType.UniqueIdentifier)
                    });
                    columns.Add(new AzureSqlColumnDto
                    {
                        Name = $"{attribute.LogicalName}_entitytype",
                        DataType = Enum.GetName(typeof(SqlDbType), SqlDbType.NVarChar),
                        DataLength = 128
                    });
                    break;
                case AttributeTypeCode.Boolean:
                    columns.Add(new AzureSqlColumnDto
                    {
                        Name = attribute.LogicalName,
                        DataType = Enum.GetName(typeof(SqlDbType), SqlDbType.Bit)
                    });
                    break;
                case AttributeTypeCode.Integer:
                case AttributeTypeCode.Picklist:
                case AttributeTypeCode.Status:
                case AttributeTypeCode.State:
                    columns.Add(new AzureSqlColumnDto
                    {
                        Name = attribute.LogicalName,
                        DataType = Enum.GetName(typeof(SqlDbType), SqlDbType.Int)
                    });
                    break;
                case AttributeTypeCode.BigInt:
                    columns.Add(new AzureSqlColumnDto
                    {
                        Name = attribute.LogicalName,
                        DataType = Enum.GetName(typeof(SqlDbType), SqlDbType.BigInt)
                    });
                    break;
                case AttributeTypeCode.Decimal:
                    var attributeDecimal = (DecimalAttributeMetadata)attribute;

                    columns.Add(new AzureSqlColumnDto
                    {
                        Name = attribute.LogicalName,
                        DataType = Enum.GetName(typeof(SqlDbType), SqlDbType.Decimal),
                        DataLength = 38,
                        DataPresition = attributeDecimal.Precision
                    });
                    break;
                case AttributeTypeCode.Double:
                    var attributeDouble = (DoubleAttributeMetadata)attribute;

                    columns.Add(new AzureSqlColumnDto
                    {
                        Name = attribute.LogicalName,
                        DataType = Enum.GetName(typeof(SqlDbType), SqlDbType.Decimal),
                        DataLength = 38,
                        DataPresition = attributeDouble.Precision
                    });
                    break;
                case AttributeTypeCode.Money:
                    var attributeMoney = (MoneyAttributeMetadata)attribute;

                    columns.Add(new AzureSqlColumnDto
                    {
                        Name = attribute.LogicalName,
                        DataType = Enum.GetName(typeof(SqlDbType), SqlDbType.Decimal),
                        DataLength = 38,
                        DataPresition = attributeMoney.Precision
                    });
                    break;
                case AttributeTypeCode.String:
                    var attributeString = (StringAttributeMetadata)attribute;

                    columns.Add(new AzureSqlColumnDto
                    {
                        Name = attribute.LogicalName,
                        DataType = Enum.GetName(typeof(SqlDbType), SqlDbType.NVarChar),
                        DataLength = attributeString.MaxLength > 8000 ? -1 : attributeString.MaxLength
                    });
                    break;
                case AttributeTypeCode.DateTime:
                    columns.Add(new AzureSqlColumnDto
                    {
                        Name = attribute.LogicalName,
                        DataType = Enum.GetName(typeof(SqlDbType), SqlDbType.DateTime)
                    });
                    break;
                case AttributeTypeCode.Memo:
                    columns.Add(new AzureSqlColumnDto
                    {
                        Name = attribute.LogicalName,
                        DataType = Enum.GetName(typeof(SqlDbType), SqlDbType.NVarChar),
                        DataLength = -1

                    });
                    break;
                case AttributeTypeCode.EntityName:
                case AttributeTypeCode.ManagedProperty:
                    columns.Add(new AzureSqlColumnDto
                    {
                        Name = attribute.LogicalName,
                        DataType = Enum.GetName(typeof(SqlDbType), SqlDbType.NVarChar),
                        DataLength = 4000
                    });
                    break;
                default:
                    _logger.LogError("Unknown type {type} for attribute {logicalName} into {entityLogicalName} metadata",
                        attribute.AttributeType, attribute.LogicalName, attribute.EntityLogicalName);
                    break;
            }
        }

        return columns;
    }
}

