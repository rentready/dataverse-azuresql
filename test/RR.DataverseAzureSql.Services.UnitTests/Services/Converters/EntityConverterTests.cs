using FakeItEasy;
using RR.DataverseAzureSql.Common.Constants;
using RR.DataverseAzureSql.Common.Dtos.Configs;
using RR.DataverseAzureSql.Common.Interfaces.Services.Configs;
using RR.DataverseAzureSql.Tests.Common.Factories;
using Xunit;

namespace RR.DataverseAzureSql.Services.UnitTests.Services.Converters;

public class EntityConverterTests
{
    [Fact]
    public void AddRequiredFields_ShouldSet_CorrectNames()
    {
        var createdOn = new DateTime(2020, 1, 1);
        var modifiedOn = createdOn.AddDays(1);
        // Arrange
        var atrbList = new List<KeyValuePair<string, object>>
        {
            new KeyValuePair<string, object>(DynamicsCommonAttributeNames.CreatedOn, createdOn),
            new KeyValuePair<string, object>(DynamicsCommonAttributeNames.ModifiedOn, modifiedOn)
        };
        var converter = EntityFactory.CreateEntityConverter();
        var document = new Dictionary<string, object>();

        // Act
        converter.AddRequiredFields(atrbList, document);

        // Assert
        Assert.Equal(createdOn, document[AzureSqlCommonAttributeNames.SinkCreatedOn]);
        Assert.Equal(modifiedOn, document[AzureSqlCommonAttributeNames.SinkModifiedOn]);
    }

    [Fact]
    public void ExcludeFieldsSpecifiedInConfig_Should_ExludeFields()
    {
        // Arrange
        var retrieveEntitySettingsConfigProvider = A.Fake<IRetrieveEntitySettingsConfigProvider>();
        var exludedFieldName = "fieldName1";
        var nonExcludedFieldName = "fieldName2";
        var settings = new RetrieveEntitySettings[]
        {
            new RetrieveEntitySettings
            {
                EntityLogicalName = EntityLogicalNames.Account,
                ExcludedFields = new string[] { exludedFieldName }
            }
        };

        A.CallTo(() => retrieveEntitySettingsConfigProvider.Get()).Returns(settings);

        var converter = EntityFactory.CreateEntityConverter(retrieveEntitySettingsConfigProvider: retrieveEntitySettingsConfigProvider);
        var document = new Dictionary<string, object>
        {
            { exludedFieldName, new object() },
            { nonExcludedFieldName, new object() },
        };

        // Act
        converter.ExcludeFieldsSpecifiedInConfig(document, EntityLogicalNames.Account);

        // Assert
        Assert.Single(document);
        Assert.True(document.ContainsKey(nonExcludedFieldName));
    }
}

