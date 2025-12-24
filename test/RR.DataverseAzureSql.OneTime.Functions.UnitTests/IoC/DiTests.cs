using System.Reflection;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using RR.DataverseAzureSql.OneTime.Functions.Functions;
using RR.DataverseAzureSql.Tests.Common.Fixtures;
using Xunit;

namespace RR.DataverseAzureSql.OneTime.Functions.UnitTests.IoC;

public class DiTests : IClassFixture<FunctionAppFixture>
{
    private readonly FunctionAppFixture _app;

    public static IEnumerable<object[]> AllAzureFunctions
    {
        get
        {
            var value = new List<object[]>();
            value.AddRange(GetAllAzureFunctions(typeof(SyncToAzureSqlHttp).Assembly));
            return value;
        }
    }

    public DiTests(FunctionAppFixture app)
    {
        _app = app;
    }

    [Theory]
    [MemberData(nameof(AllAzureFunctions))]
    public void AzureFunctionDi_Should_Work(Type functionType)
    {
        // Assert
        var function = _app.Host.Services.GetRequiredService(serviceType: functionType);
        Assert.NotNull(function);
    }

    private static IEnumerable<object[]> GetAllAzureFunctions(Assembly assembly)
    {
        foreach (var azureFunctionClassType in assembly.GetTypes().Where(t => t.IsPublic))
        {
            if (azureFunctionClassType.GetMethods().Any(info => info.IsPublic &&
                    info.GetCustomAttributes().Any(attr => attr is FunctionAttribute)))
            {
                yield return new object[] { azureFunctionClassType };
            }
        }
    }
}

