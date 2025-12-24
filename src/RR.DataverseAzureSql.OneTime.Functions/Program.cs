using Microsoft.Extensions.Hosting;
using RR.DataverseAzureSql.OneTime.Functions.Extensions.IoC;
using RR.DataverseAzureSql.Services.Extensions.IoC;

namespace RR.DataverseAzureSql.OneTime.Functions;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args);
        await host.Build().RunAsync();
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureFunctionsWorkerDefaults(worker => worker.UseNewtonsoftJson())
            .ConfigureServices(services => services.RegisterServices());
}

