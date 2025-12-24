using FakeItEasy;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using RR.DataverseAzureSql.Common.Constants;
using RR.DataverseAzureSql.Tests.Common.Factories;
using Xunit;

namespace RR.DataverseAzureSql.Services.UnitTests.Services.Metadata
{
    public class DynamicsEntityAttributesMetadataServiceTests
    {
        [Fact]
        public async Task Metadata_ShouldBe_Cached()
        {
            // Arrange
            var fakeOrganizationService = A.Fake<IOrganizationServiceAsync2>();

            var expectedEntityMetadata1 = new EntityMetadata()
            {
                EntitySetName = "EntitySetName1"
            };

            var expectedEntityMetadata2 = new EntityMetadata()
            {
                EntitySetName = "EntitySetName2"
            };
            int i = 0;
            A.CallTo(() => fakeOrganizationService.ExecuteAsync(A<OrganizationRequest>.Ignored))
                .ReturnsLazily((OrganizationRequest request) =>
                {
                    var parCol = new ParameterCollection();
                    if (i == 0)
                    {
                        parCol.Add("EntityMetadata", expectedEntityMetadata1);
                    }
                    else
                    {
                        parCol.Add("EntityMetadata", expectedEntityMetadata2);
                    }
                    i += 1;
                    OrganizationResponse response = new RetrieveEntityResponse()
                    {
                        Results = parCol
                    };
                    return Task.FromResult(response);
                });
            // Act
            var service = EntityFactory.CreateDynamicsEntityAttributesMetadataService(fakeOrganizationService);
            var actualEntityMetadata1 = await service.GetAsync(EntityLogicalNames.Account);
            var actualEntityMetadata2 = await service.GetAsync(EntityLogicalNames.Account);

            // Assert
            Assert.Equal(actualEntityMetadata1.EntitySetName, actualEntityMetadata2.EntitySetName);
            Assert.Equal(expectedEntityMetadata1.EntitySetName, actualEntityMetadata2.EntitySetName);
        }
    }
}
