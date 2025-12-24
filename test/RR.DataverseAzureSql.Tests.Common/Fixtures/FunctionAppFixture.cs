using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using RR.DataverseAzureSql.OneTime.Functions.Functions;
using RR.DataverseAzureSql.OneTime.Functions.Functions.Durable;
using RR.DataverseAzureSql.RealTime.Functions.Functions;
using RR.DataverseAzureSql.Services.Extensions.IoC;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace RR.DataverseAzureSql.Tests.Common.Fixtures;

public class FunctionAppFixture
{
    public IHost Host { get; private set; }

    public FunctionAppFixture()
    {
        InitialConfiguration.SetEnvironmentVariables();

        Host = new HostBuilder()
            .ConfigureFunctionsWorkerDefaults()
            .ConfigureServices(services =>
            {
                services.RegisterServices();
                services.AddScoped<SyncToAzureSql>();
                services.AddScoped<SyncToAzureSqlHttp>();
                services.AddScoped<SyncToAzureSqlCron>();
                services.AddScoped<SyncToAzureSqlOrchestrator>();
                services.AddScoped<SyncToAzureSqlWorker>();
                services.AddScoped<SyncToAzureSqlActivity>();
                services.AddScoped<CleanUpAzureSqlTableActivity>();
                services.AddScoped<CreateOrUpdateAzureSqlTableSchemaActivity>();
                services.Replace(ServiceDescriptor.Singleton(typeof(IOrganizationService), A.Fake<IOrganizationService>()));
                services.Replace(ServiceDescriptor.Singleton(typeof(IOrganizationServiceAsync), A.Fake<IOrganizationServiceAsync>()));
            })
            .Build();
    }
}

