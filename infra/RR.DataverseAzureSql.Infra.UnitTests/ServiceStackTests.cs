using Pulumi;
using Pulumi.AzureNative.Web;
using RR.PulumiExt;
using System.Collections.Immutable;
using Xunit;

namespace RR.DataverseAzureSql.Infra.UnitTests;

public class ServiceStackTests
{
    public static IEnumerable<object[]> AllStackNames =>
        new[]
        {
                new object[] { "prod" },
                new object[] { "dev" },
                new object[] { "sandbox" }
        };

    [Theory]
    [MemberData(nameof(AllStackNames))]
    public async Task When_Pulumi_Up_has_been_ran_Then_correct_output(string stackName)
    {
        // Arrange & Act
        var resources = await RunAsync(stackName);

        // Assert
        var webApps = resources.OfType<WebApp>().ToArray();
        Assert.Equal(2, webApps.Length);
    }

    private static async Task<ImmutableArray<Resource>> RunAsync(string stackName)
    {
        return await Testing.RunAsync<ServiceStack>("dataverse-azuresql", stackName);
    }
}

