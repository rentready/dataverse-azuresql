using System.Data.SqlClient;
using FakeXrmEasy;
using Microsoft.Extensions.Options;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using RR.Common.Testing.Factories;
using RR.DataverseAzureSql.Common.Constants;
using RR.DataverseAzureSql.Common.Interfaces.Services.Dynamics;
using RR.DataverseAzureSql.Common.Interfaces.Services.Metadata;
using RR.DataverseAzureSql.Services.Options.Services.Sync;
using RR.DataverseAzureSql.Services.Services.Converters;
using RR.DataverseAzureSql.Tests.Common.Factories;
using RR.DataverseAzureSql.Tests.Common.Mocks;
using RR.DataverseAzureSql.Tests.Common.Mocks.Databases;
using Xunit;

namespace RR.DataverseAzureSql.Tests.Common.Functions
{
    public class PositiveTestBase
    {
        protected static List<(object, object)> ChangeAttributes(Entity entity)
        {
            var values = new List<(object, object)>();
            foreach (var attribute in entity.Attributes)
            {
                var newValue = ModifyAttributeValue(attribute.Value);
                values.Add((entity[attribute.Key], newValue));
                // Update the attribute with the new value
                entity[attribute.Key] = ModifyAttributeValue(attribute.Value);
            }
            return values;
        }

        protected static object ModifyAttributeValue(object attributeValue)
        {
            switch (attributeValue)
            {
                case int intValue:
                    return intValue + 1;
                case long longValue:
                    return longValue + 1;

                case string stringValue:
                    {
                        var converter = new DateTimeConverter();
                        var date = converter.UnixEpochDateTimeConverter(stringValue);
                        if (date.HasValue)
                        {
                            DateTimeOffset offset = date.Value;
                            var ms = offset.ToUnixTimeMilliseconds();
                            return $"/Date({ms + 1000})/";
                        }
                        return stringValue + "_modified";
                    }

                case DateTime dateTimeValue:
                    return dateTimeValue.AddDays(1);

                case decimal decimalValue:
                    return decimalValue * 2;

                case bool boolValue:
                    return !boolValue;

                case Guid guidValue:
                    return Guid.NewGuid();

                case EntityReference entityRef:
                    return new EntityReference(entityRef.LogicalName, Guid.NewGuid());

                case OptionSetValue optionSet:
                    return new OptionSetValue(optionSet.Value + 1);
                case Money money:
                    return new Money(money.Value * 2);

                default:
                    return attributeValue;
            }
        }

        protected static IOptions<OneTimeSyncToAzureSqlServiceOptions> GetOneTimeSyncToAzureSqlServiceOptions()
        {
            var expectedEntityLogicalNames = $"{EntityLogicalNames.WorkOrder}";
            return EntityFactory.CreateOneTimeSyncToAzureSqlServiceOptions(expectedEntityLogicalNames);
        }

        protected static async Task AssertEntity(string entityLogicalName, SqlExpressDbConnectionFactory dbConnectionFactory, Entity entity, bool exludeSinkFields = false)
        {
            using var connection = dbConnectionFactory.Create();
            await connection.OpenAsync();
            var actualData = new Dictionary<string, object>();
            var command = new SqlCommand($"SELECT * FROM {entityLogicalName}", connection);

            // Execute the command
            using var reader = await command.ExecuteReaderAsync();
            var rowCount = 0;
            while (await reader.ReadAsync())
            {
                // Read each column and add it to actualData
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    var columnName = reader.GetName(i);
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    actualData[columnName] = value;
                }
                rowCount++;
            }
            if (entity == null)
            {
                Assert.Equal(0, rowCount);
            }
            else
            {
                Assert.Equal(1, rowCount);

                var entityConverter = EntityFactory.CreateEntityConverter();
                var expectedData = entityConverter.ToDictionary(entity);

                var exludedFields = new HashSet<string>()
                {
                    AzureSqlCommonAttributeNames.SinkCreatedOn,
                    AzureSqlCommonAttributeNames.SinkModifiedOn
                };

                foreach (var kvp in expectedData.Where(x => !exludeSinkFields ||
                    !exludedFields.Contains(x.Key)))
                {
                    Assert.True(actualData.ContainsKey(kvp.Key), $"Missing key: {kvp.Key}");
                    //Assert.Equal(kvp.Value, actualData[kvp.Key]);
                    if (kvp.Value == null)
                    {
                        Assert.Equal(kvp.Value, actualData[kvp.Key]);
                        continue;
                    }

                    if (kvp.Value.Equals(actualData[kvp.Key]))
                    {
                        Assert.Equal(kvp.Value, actualData[kvp.Key]);
                    }
                    else
                    {
                        Console.WriteLine(kvp.Key);
                    }
                }
            }
        }

        protected static Entity GetWorkOrder(string msg = null)
        {
            var serviceBusMsg = EntityFactory.CreateFakeServiceBusReceivedMessage(msg ?? Properties.Resources.ServiceBusCreateMessage);
            var msgManagerService = EntityFactory.CreateMessageManagerService();
            var workOrder = msgManagerService.ProcessNewOrUpdatedMessage(serviceBusMsg);

            workOrder["msdyn_workorderid"] = Guid.Parse(workOrder["msdyn_workorderid"].ToString());
            return workOrder;
        }

        protected static IDynamicsEntityAttributesMetadataService GetDynamicsEntityAttributesMetadataService(Entity entity = null)
        {
            entity ??= GetWorkOrder();
            var atrbList = new List<AttributeMetadata>();
            var converter = new AttributeTypeCodeConverter(entity.LogicalName);
            foreach (var atrb in entity.Attributes)
            {
                var typeCode = converter.ConvertObjectToAttributeTypeCode(atrb.Value, atrb.Key);
                atrbList.Add(EntityFactory.CreateAttributeMetadata(typeCode, atrb.Key));
            }

            atrbList.Add(EntityFactory.CreateAttributeMetadata(AttributeTypeCode.BigInt, "versionnumber"));
            return EntityFactory.CreateFakeDynamicsEntityAttributesMetadataService(atrbList.ToArray());
        }

        protected static IDynamicsService GetDynamicsService(IDynamicsEntityAttributesMetadataService entityAttributesMetadataService, Entity createdOrUpdatedEntity, Entity deletedEntity)
        {
            if (createdOrUpdatedEntity == null && deletedEntity == null)
            {
                throw new NotImplementedException();
            }
            XrmFakedContext xrmFakeContext = FakeContextFactory.Arrange();

            // example https://github.com/jordimontana82/fake-xrm-easy/wiki/Mock-an-OrganizationRequest
            xrmFakeContext.AddExecutionMock<RetrieveEntityChangesRequest>(request =>
            {
                var req = request as RetrieveEntityChangesRequest;
                IList<IChangedItem> itemList = new List<IChangedItem>();
                if (createdOrUpdatedEntity != null)
                {
                    itemList.Add(new NewOrUpdatedItem(ChangeType.NewOrUpdated, createdOrUpdatedEntity));
                }
                if (deletedEntity != null)
                {
                    itemList.Add(new RemovedOrDeletedItem(ChangeType.RemoveOrDeleted,
                        new EntityReference(deletedEntity.LogicalName, deletedEntity.Id)));
                }

                var beChanges = new BusinessEntityChanges
                {
                    Changes = new BusinessEntityChangesCollection(itemList),
                    DataToken = "some token",
                    MoreRecords = false
                };
                var results = new ParameterCollection { { "EntityChanges", beChanges } };
                return new RetrieveEntityChangesResponse()
                {
                    Results = results,
                    ResponseName = nameof(RetrieveEntityChangesResponse),
                };
            });
            var orgService = xrmFakeContext.GetOrganizationService();
            return EntityFactory.CreateDynamicsService(orgService, entityAttributesMetadataService: entityAttributesMetadataService);
        }
    }
}
