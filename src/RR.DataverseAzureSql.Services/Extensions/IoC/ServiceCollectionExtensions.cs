using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RR.Common.Interfaces;
using RR.Common.Logic.Di;
using RR.Common.ServiceBus.Di;
using RR.Common.Services;
using RR.DataverseAzureSql.Common.Interfaces.Services.AzureSql;
using RR.DataverseAzureSql.Common.Interfaces.Services.Configs;
using RR.DataverseAzureSql.Common.Interfaces.Services.Converters;
using RR.DataverseAzureSql.Common.Interfaces.Services.Db;
using RR.DataverseAzureSql.Common.Interfaces.Services.Dynamics;
using RR.DataverseAzureSql.Common.Interfaces.Services.Metadata;
using RR.DataverseAzureSql.Common.Interfaces.Services.ServiceBus;
using RR.DataverseAzureSql.Common.Interfaces.Services.Storage;
using RR.DataverseAzureSql.Common.Interfaces.Services.Sync;
using RR.DataverseAzureSql.Common.Interfaces.Services.Telemetry;
using RR.DataverseAzureSql.Services.Options.Services.AzureSql;
using RR.DataverseAzureSql.Services.Options.Services.ServiceBus;
using RR.DataverseAzureSql.Services.Options.Services.Storage;
using RR.DataverseAzureSql.Services.Options.Services.Sync;
using RR.DataverseAzureSql.Services.Services.AzureSql;
using RR.DataverseAzureSql.Services.Services.Configs;
using RR.DataverseAzureSql.Services.Services.Converters;
using RR.DataverseAzureSql.Services.Services.Db;
using RR.DataverseAzureSql.Services.Services.Dynamics;
using RR.DataverseAzureSql.Services.Services.Metadata;
using RR.DataverseAzureSql.Services.Services.ServiceBus;
using RR.DataverseAzureSql.Services.Services.Storage;
using RR.DataverseAzureSql.Services.Services.Sync;
using RR.DataverseAzureSql.Services.Services.Telemetry;

namespace RR.DataverseAzureSql.Services.Extensions.IoC;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterServices(this IServiceCollection services)
    {
        services.AddSingleton<ICurrentTimeService, CurrentTimeService>();
        services.AddLogging();
        services.RegisterSyncServices();
        services.RegisterConverters();
        services.RegisterAzureSqlServices();
        services.RegisterServiceBusServices();
        services.RegisterStorageServices();
        services.RegisterMetadataServices();
        services.RegisterDynamicsServices();
        services.RegisterConfigs();
        services.RegisterTelemetry();

        return services;
    }

    private static void RegisterSyncServices(this IServiceCollection services)
    {
        services.AddSingleton<IOneTimeSyncToAzureSqlService, OneTimeSyncToAzureSqlService>();
        services.AddSingleton<IRealTimeSyncToAzureSqlService, RealTimeSyncToAzureSqlService>();
    }

    private static void RegisterConverters(this IServiceCollection services)
    {
        services.AddSingleton<IDateTimeConverter, DateTimeConverter>();
        services.AddSingleton<IEntityConverter, EntityConverter>();
    }

    private static void RegisterAzureSqlServices(this IServiceCollection services)
    {
        services.AddSingleton<IAzureSqlService, AzureSqlService>();
        services.AddSingleton<IAzureSqlFullSyncService, AzureSqlFullSyncService>();
        services.AddSingleton<IAzureSqlChangesSyncService, AzureSqlChangesSyncService>();
        services.AddSingleton<IQueryBuilderService, QueryBuilderService>();
        services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
        services.AddSingleton<ISqlSchemaService, SqlSchemaService>();
    }

    private static void RegisterMetadataServices(this IServiceCollection services)
    {
        services.AddSingleton<IAzureSqlEntityAttributesMetadataService, AzureSqlEntityAttributesMetadataService>();
        services.AddSingleton<IDynamicsEntityAttributesMetadataService, DynamicsEntityAttributesMetadataService>();
    }

    private static void RegisterServiceBusServices(this IServiceCollection services)
    {
        services.AddSingleton<IServiceBusService, ServiceBusService>();
        services.AddSingleton<IMessageManagerService, MessageManagerService>();
        services.RegisterServiceBusClient<DataverseAzureSqlServiceBusOptions>();
    }

    private static void RegisterStorageServices(this IServiceCollection services)
    {
        services.AddAzureClients(builder =>
        {
            builder.AddClient<TableClient, TableClientOptions>((_, _, provider) =>
            {
                var options = provider.GetService<IOptions<TableServiceOptions>>().Value;

                return new TableClient(options.ConnectionString, options.TableName);
            })
            .WithName(typeof(TableServiceOptions).ToString().Split('.')[^1]);
        });

        services.AddSingleton<ITableService, TableService>();
    }

    private static void RegisterDynamicsServices(this IServiceCollection services)
    {
        services.RegisterOrganizationService();
        services.AddSingleton<IDynamicsService, DynamicsService>();
    }

    public static void AddOptions(this IServiceCollection services)
    {
        services.AddOptions<OneTimeSyncToAzureSqlServiceOptions>()
            .Configure<IConfiguration>((settings, configuration) =>
            {
                configuration.GetSection(nameof(OneTimeSyncToAzureSqlServiceOptions)).Bind(settings);
            })
            .ValidateDataAnnotations();

        services.AddOptions<RealTimeSyncToAzureSqlServiceOptions>()
            .Configure<IConfiguration>((settings, configuration) =>
            {
                configuration.GetSection(nameof(RealTimeSyncToAzureSqlServiceOptions)).Bind(settings);
            })
            .ValidateDataAnnotations();

        services.AddOptions<AzureSqlServiceOptions>()
            .Configure<IConfiguration>((settings, configuration) =>
            {
                configuration.GetSection(nameof(AzureSqlServiceOptions)).Bind(settings);
            })
            .ValidateDataAnnotations();

        services.AddOptions<AzureSqlFullSyncServiceOptions>()
            .Configure<IConfiguration>((settings, configuration) =>
            {
                configuration.GetSection(nameof(AzureSqlFullSyncServiceOptions)).Bind(settings);
            })
            .ValidateDataAnnotations();

        services.AddOptions<AzureSqlChangesSyncServiceOptions>()
            .Configure<IConfiguration>((settings, configuration) =>
            {
                configuration.GetSection(nameof(AzureSqlChangesSyncServiceOptions)).Bind(settings);
            })
            .ValidateDataAnnotations();

        services.AddOptions<TableServiceOptions>()
            .Configure<IConfiguration>((settings, configuration) =>
            {
                configuration.GetSection(nameof(TableServiceOptions)).Bind(settings);
            })
            .ValidateDataAnnotations();
    }

    public static void RegisterConfigs(this IServiceCollection services)
    {
        services.AddSingleton<IRetrieveEntitySettingsConfigProvider, RetrieveEntitySettingsConfigProvider>();

        services.AddOptions();
    }

    public static void RegisterTelemetry(this IServiceCollection services)
    {
        services.AddSingleton<ICustomMetricsService, CustomMetricsService>();
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
    }
}

