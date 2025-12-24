using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;

namespace RR.DataverseAzureSql.OneTime.Functions.Extensions.IoC;

public static class FunctionsWorkerApplicationBuilderExtensions
{
    public static IFunctionsWorkerApplicationBuilder UseNewtonsoftJson(this IFunctionsWorkerApplicationBuilder builder)
    {
        builder.Services.Configure<WorkerOptions>(workerOptions =>
        {
            var settings = NewtonsoftJsonObjectSerializer.CreateJsonSerializerSettings();
            workerOptions.Serializer = new NewtonsoftJsonObjectSerializer(settings);
        });

        return builder;
    }
}

