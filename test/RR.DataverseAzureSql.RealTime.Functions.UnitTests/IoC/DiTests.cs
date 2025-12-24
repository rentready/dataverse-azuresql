using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using RR.DataverseAzureSql.RealTime.Functions.Functions;
using RR.DataverseAzureSql.Tests.Common.Fixtures;
using System.Reflection;
using Xunit;

namespace RR.DataverseAzureSql.RealTime.Functions.UnitTests.IoC;

public class DiTests : IClassFixture<FunctionAppFixture>
{
    private readonly FunctionAppFixture _app;

    public static IEnumerable<object[]> AllAzureFunctions
    {
        get
        {
            return GetAllAzureFunctions(typeof(SyncToAzureSql).Assembly);
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

