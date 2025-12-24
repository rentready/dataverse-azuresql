using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Azure.Core.Serialization;
using Azure.Messaging.ServiceBus;
using FakeItEasy;
using FakeXrmEasy;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Newtonsoft.Json;
using RR.Common.Interfaces;
using RR.Common.Services;
using RR.Common.Testing;
using RR.Common.Testing.Factories;
using RR.DataverseAzureSql.Common.Constants;
using RR.DataverseAzureSql.Common.Dtos.Configs;
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
using RR.DataverseAzureSql.OneTime.Functions.Functions;
using RR.DataverseAzureSql.OneTime.Functions.Functions.Durable;
using RR.DataverseAzureSql.RealTime.Functions.Functions;
using RR.DataverseAzureSql.Services.Options.Services.AzureSql;
using RR.DataverseAzureSql.Services.Options.Services.Sync;
using RR.DataverseAzureSql.Services.Services.AzureSql;
using RR.DataverseAzureSql.Services.Services.Configs;
using RR.DataverseAzureSql.Services.Services.Converters;
using RR.DataverseAzureSql.Services.Services.Dynamics;
using RR.DataverseAzureSql.Services.Services.Metadata;
using RR.DataverseAzureSql.Services.Services.ServiceBus;
using RR.DataverseAzureSql.Services.Services.Sync;
using RR.DataverseAzureSql.Services.Services.Telemetry;
using RR.DataverseAzureSql.Tests.Common.Mocks;
using RR.DataverseAzureSql.Tests.Common.Mocks.Databases;
using RR.DataverseAzureSql.Tests.Common.Mocks.Isolated;
using RR.DataverseAzureSql.Tests.Common.Mocks.Services.AzureSql;
using RR.DataverseAzureSql.Tests.Common.Mocks.Services.Storage;
using RR.DataverseAzureSql.Tests.Common.Mocks.Telemetry;

namespace RR.DataverseAzureSql.Tests.Common.Factories;

public static class EntityFactory
{

    private static readonly RetrieveEntitySettingsConfigProvider _defaultRetrieveEntitySettingsConfigProvider
        = GetRetrieveEntitySettingsConfigProvider();
    public static MessageManagerService CreateMessageManagerService(IOptions<RealTimeSyncToAzureSqlServiceOptions> options = null)
    {
        return new MessageManagerService(options ?? CreateRealTimeSyncToAzureSqlServiceOptions());
    }

    public static FakeOrganizationalServiceAsync2 CreateFakeOrganizationalServiceAsync2(XrmFakedContext context)
    {
        return new FakeOrganizationalServiceAsync2(context.GetOrganizationService());
    }

    public static DateTimeConverter CreateDateTimeConverter()
    {
        return new DateTimeConverter();
    }

    public static DynamicsEntityAttributesMetadataService CreateDynamicsEntityAttributesMetadataService(IOrganizationServiceAsync organizationService)
    {
        return new DynamicsEntityAttributesMetadataService(organizationService);
    }

    public static IDynamicsEntityAttributesMetadataService CreateFakeDynamicsEntityAttributesMetadataService(AttributeMetadata[] attributeMetadatas)
    {
        var entityAttributesMetadataService = A.Fake<IDynamicsEntityAttributesMetadataService>();
        var metaData = new EntityMetadata();
        var propAttributes = metaData.GetType().GetField("_attributes", BindingFlags.NonPublic
            | BindingFlags.Instance);
        propAttributes.SetValue(metaData, attributeMetadatas);
        A.CallTo(() => entityAttributesMetadataService.GetAsync(A<string>.Ignored)).Returns(Task.FromResult(metaData));
        return entityAttributesMetadataService;
    }

    public static AttributeMetadata CreateAttributeMetadata(AttributeTypeCode code, string logicalName,
            params string[] lookupTargets)
    {
        if (code == AttributeTypeCode.Lookup || code == AttributeTypeCode.Customer)
        {
            var lookup = new LookupAttributeMetadata
            {
                LogicalName = logicalName,
                Targets = lookupTargets != null && lookupTargets.Length > 0 ? lookupTargets : new string[] { "account" },

            };
            if (code == AttributeTypeCode.Customer)
            {
                var field = typeof(AttributeMetadata).GetField("_attributeType", BindingFlags.NonPublic
                    | BindingFlags.Instance);
                field.SetValue(lookup, code);
            }
            return lookup;
        }

        if (code == AttributeTypeCode.String)
        {
            return new StringAttributeMetadata
            {
                LogicalName = logicalName,
                MaxLength = 4000
            };
        }

        if (code == AttributeTypeCode.Money)
        {
            return new MoneyAttributeMetadata
            {
                LogicalName = logicalName,
                Precision = 2
            };
        }

        if (code == AttributeTypeCode.Double)
        {
            return new DoubleAttributeMetadata
            {
                LogicalName = logicalName,
                Precision = 4
            };
        }

        if (code == AttributeTypeCode.Decimal)
        {
            return new DecimalAttributeMetadata
            {
                LogicalName = logicalName,
                Precision = 4
            };
        }

        var constructorInfo = typeof(AttributeMetadata).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single(x => x.GetParameters().Length == 1);
        var result = (AttributeMetadata)constructorInfo.Invoke(new object[] { code });
        result.LogicalName = logicalName;
        return result;
    }

    public static ServiceBusReceivedMessage CreateFakeServiceBusReceivedMessage(string messageBody,
        Dictionary<string, object> properties = null)
    {
        return ServiceBusModelFactory.ServiceBusReceivedMessage(new BinaryData(messageBody),
               properties: properties);
    }

    public static FakeServiceBusMessageActions CreateFakeServiceBusMessageActions()
    {
        return new FakeServiceBusMessageActions();
    }

    internal static CurrentTimeService CreateCurrentTimeService()
    {
        return new CurrentTimeService();
    }

    public static OneTimeSyncToAzureSqlService CreateOneTimeSyncToAzureSqlService(
        IDynamicsService dynamicsService,
        IAzureSqlFullSyncService azureSqlFullSyncService,
        IAzureSqlChangesSyncService azureSqlChangesSyncService,
        ITableService tableService = null,
        IEntityConverter entityConverter = null,
        ILogger<OneTimeSyncToAzureSqlService> logger = null)
    {
        return new OneTimeSyncToAzureSqlService(dynamicsService,
            azureSqlFullSyncService,
            azureSqlChangesSyncService,
            tableService ?? CreateMockTableService(),
            entityConverter ?? CreateEntityConverter(),
            logger ?? A.Fake<ILogger<OneTimeSyncToAzureSqlService>>());
    }

    public static EntityConverter CreateEntityConverter(IDateTimeConverter dateTimeConverter = null,
        ICurrentTimeService currentTimeService = null,
        IRetrieveEntitySettingsConfigProvider retrieveEntitySettingsConfigProvider = null)
    {
        return new EntityConverter(dateTimeConverter ?? CreateDateTimeConverter(),
            currentTimeService ?? CreateCurrentTimeService(),
            retrieveEntitySettingsConfigProvider ?? GetRetrieveEntitySettingsConfigProvider());
    }

    public static RealTimeSyncToAzureSqlService CreateRealTimeSyncToAzureSqlService(IDynamicsService dynamicsService = null,
        IAzureSqlService azureSqlService = null,
        IServiceBusService serviceBusService = null,
        IMessageManagerService messageManagerService = null,
        IEntityConverter entityConverter = null,
        IOptions<RealTimeSyncToAzureSqlServiceOptions> options = null,
        ISqlSchemaService sqlSchemaService = null,
        ICustomMetricsService customMetricsService = null,
        ILogger<RealTimeSyncToAzureSqlService> logger = null)
    {
        var realTimeSyncToAzureSqlServiceOptions = CreateRealTimeSyncToAzureSqlServiceOptions();

        return new RealTimeSyncToAzureSqlService(dynamicsService ?? A.Fake<IDynamicsService>(),
            azureSqlService ?? A.Fake<IAzureSqlService>(),
            serviceBusService ?? A.Fake<IServiceBusService>(),
            messageManagerService ?? CreateMessageManagerService(realTimeSyncToAzureSqlServiceOptions),
            entityConverter ?? CreateEntityConverter(),
            options ?? realTimeSyncToAzureSqlServiceOptions,
            sqlSchemaService ?? A.Fake<ISqlSchemaService>(),
            customMetricsService ?? A.Fake<ICustomMetricsService>(),
            logger ?? A.Fake<ILogger<RealTimeSyncToAzureSqlService>>());
    }

    public static IOptions<OneTimeSyncToAzureSqlServiceOptions> CreateOneTimeSyncToAzureSqlServiceOptions()
    {
        return CreateOneTimeSyncToAzureSqlServiceOptions("account");
    }

    public static IOptions<RealTimeSyncToAzureSqlServiceOptions> CreateRealTimeSyncToAzureSqlServiceOptions()
    {
        return Options.Create(new RealTimeSyncToAzureSqlServiceOptions
        {
            MaxMessageRetryCount = 10,
            RelationshipEntityLogicalNames = "account_contact"
        });
    }

    public static IOptions<AzureSqlChangesSyncServiceOptions> CreateAzureSqlChangesSyncServiceOptions()
    {
        return Options.Create(new AzureSqlChangesSyncServiceOptions
        {
            DeleteBatchSize = 10,
            InsertBatchSize = 10,
            UpdateBatchSize = 10,
        });
    }

    public static SyncToAzureSqlHttp CreateSyncToAzureSqlHttp(IOptions<OneTimeSyncToAzureSqlServiceOptions> options)
    {
        return new SyncToAzureSqlHttp(options);
    }

    public static SyncToAzureSqlOrchestrator CreateSyncToAzureSqlOrchestrator()
    {
        return new SyncToAzureSqlOrchestrator();
    }

    public static SyncToAzureSqlWorker CreateSyncToAzureSqlWorker(IOptions<OneTimeSyncToAzureSqlServiceOptions> options,
        ILogger<SyncToAzureSqlWorker> logger = null)
    {
        return new SyncToAzureSqlWorker(options, logger ?? A.Fake<ILogger<SyncToAzureSqlWorker>>());
    }

    public static CleanUpAzureSqlTableActivity CreateCleanUpAzureSqlTableActivity(
        IAzureSqlFullSyncService azureSqlFullSyncService,
        ILogger<CleanUpAzureSqlTableActivity> logger = null
        )
    {
        return new CleanUpAzureSqlTableActivity(azureSqlFullSyncService,
            logger ?? A.Fake<ILogger<CleanUpAzureSqlTableActivity>>());
    }

    public static CreateOrUpdateAzureSqlTableSchemaActivity CreateCreateOrUpdateAzureSqlTableSchemaActivity(
        ISqlSchemaService sqlSchemaService,
        ILogger<CreateOrUpdateAzureSqlTableSchemaActivity> logger = null
        )
    {
        return new CreateOrUpdateAzureSqlTableSchemaActivity(sqlSchemaService,
            logger ?? A.Fake<ILogger<CreateOrUpdateAzureSqlTableSchemaActivity>>());
    }

    public static SqlSchemaService CreateSqlSchemaService(IDynamicsEntityAttributesMetadataService dynamicsMetadataService,
        ISqlConnectionFactory sqlConnectionFactory,
        IAzureSqlEntityAttributesMetadataService azureSqlEntityAttributesMetadataService = null,
        IQueryBuilderService queryBuilderService = null,
        ILogger<SqlSchemaService> logger = null)
    {
        return new SqlSchemaService(dynamicsMetadataService,
            queryBuilderService ?? CreateQueryBuilderService(), sqlConnectionFactory,
            azureSqlEntityAttributesMetadataService ?? CreateAzureSqlEntityAttributesMetadataService(),
            logger ?? A.Fake<ILogger<SqlSchemaService>>());
    }

    public static AzureSqlEntityAttributesMetadataService CreateAzureSqlEntityAttributesMetadataService(
        ILogger<AzureSqlEntityAttributesMetadataService> logger = null)
    {
        return new AzureSqlEntityAttributesMetadataService(logger ?? A.Fake<ILogger<AzureSqlEntityAttributesMetadataService>>());
    }

    public static SyncToAzureSqlActivity CreateSyncToAzureSqlActivity(
        IOneTimeSyncToAzureSqlService oneTimeSyncToAzureSqlService,
        ILogger<SyncToAzureSqlActivity> logger = null
        )
    {
        return new SyncToAzureSqlActivity(oneTimeSyncToAzureSqlService, logger ?? A.Fake<ILogger<SyncToAzureSqlActivity>>());
    }

    public static AzureSqlFullSyncService CreateAzureSqlFullSyncService(
        ISqlConnectionFactory connectionFactory,
        IOptions<AzureSqlFullSyncServiceOptions> fullSyncServiceOptions,
        IQueryBuilderService queryBuilderService = null,
        ILogger<AzureSqlFullSyncService> logger = null)
    {
        return new AzureSqlFullSyncService(queryBuilderService ?? CreateQueryBuilderService(),
            connectionFactory, fullSyncServiceOptions,
            logger ?? A.Fake<ILogger<AzureSqlFullSyncService>>());
    }

    public static AzureSqlChangesSyncService CreateAzureSqlChangesSyncService(
        ISqlConnectionFactory connectionFactory,
        IOptions<AzureSqlChangesSyncServiceOptions> changesSyncServiceOptions = null,
        IQueryBuilderService queryBuilderService = null,
        ILogger<AzureSqlChangesSyncService> logger = null)
    {
        return new AzureSqlChangesSyncService(queryBuilderService ?? CreateQueryBuilderService(),
            connectionFactory,
            changesSyncServiceOptions ?? CreateAzureSqlChangesSyncServiceOptions(),
            logger ?? A.Fake<ILogger<AzureSqlChangesSyncService>>());
    }

    public static QueryBuilderService CreateQueryBuilderService(ILogger<QueryBuilderService> logger = null)
    {
        return new QueryBuilderService(logger ?? A.Fake<ILogger<QueryBuilderService>>());
    }

    public static SyncToAzureSqlCron CreateSyncToAzureSqlCron(
        ICustomMetricsService customMetricsService,
        IOptions<OneTimeSyncToAzureSqlServiceOptions> options,
        ILogger<SyncToAzureSqlCron> logger = null)
    {
        return new SyncToAzureSqlCron(customMetricsService,
            options, logger ?? A.Fake<ILogger<SyncToAzureSqlCron>>());
    }

    public static IOptions<OneTimeSyncToAzureSqlServiceOptions> CreateOneTimeSyncToAzureSqlServiceOptions(
        string synchronizedEntityLogicalNames)
    {
        var options = new OneTimeSyncToAzureSqlServiceOptions
        {
            SynchronizedEntityLogicalNames = synchronizedEntityLogicalNames,
            BatchSize = 2500,
            MaxActivityRetryCount = 3,
            ActivityRetryIntervalInSec = 30,
        };
        return Options.Create(options);
    }

    public static IOptions<AzureSqlServiceOptions> CreateAzureSqlServiceOptions(
        string connectionString)
    {
        var options = new AzureSqlServiceOptions
        {
            ConnectionString = connectionString
        };
        return Options.Create(options);
    }

    public static SqlExpressDbConnectionFactory CreateSqlExpressDbConnectionFactory([CallerMemberName] string caller = null)
    {
        return new SqlExpressDbConnectionFactory(caller);
    }

    public static IOptions<AzureSqlFullSyncServiceOptions> CreateAzureSqlFullSyncServiceOptions()
    {
        var options = new AzureSqlFullSyncServiceOptions
        {
            BatchSize = 1000,
            TimeoutInSec = 30,
        };
        return Options.Create(options);
    }

    public static DynamicsService CreateDynamicsService(IOrganizationService organizationService,
        IDynamicsEntityAttributesMetadataService entityAttributesMetadataService,
        IRetrieveEntitySettingsConfigProvider retrieveEntitySettingsConfigProvider = null,
        ITableService tableService = null, IOptions<OneTimeSyncToAzureSqlServiceOptions> options = null)
    {
        return new DynamicsService(organizationService,
            tableService ?? CreateMockTableService(),
            options ?? CreateOneTimeSyncToAzureSqlServiceOptions(),
            entityAttributesMetadataService,
            retrieveEntitySettingsConfigProvider ?? _defaultRetrieveEntitySettingsConfigProvider
            );
    }

    public static RetrieveEntitySettingsConfigProvider GetRetrieveEntitySettingsConfigProvider()
    {
        var inMemoryConfig = new Dictionary<string, string>
        {
            {ConfigSectionNames.RetrieveEntitySettings, RetrieveEntitySettingsFactory.GetDefaultSettings()},
        };
        var configuration = FakeEntityFactory.GetFakeConfiguration(inMemoryConfig);
        return new RetrieveEntitySettingsConfigProvider(configuration);
    }

    public static MockHttpRequestData CreateHttpRequestData<T>(T payload = null, bool serializeNullValues = true, string method = "GET")
            where T : class
    {
        string json;
        if (serializeNullValues)
        {
            json = JsonConvert.SerializeObject(payload);
        }
        else
        {
            json = JsonConvert.SerializeObject(payload, Formatting.Indented, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
        }
        return CreateHttpRequestDataString(json, method: method);
    }

    public static MockHttpRequestData CreateHttpRequestDataString(string payload = null,
        string token = null,
        string method = "GET")
    {
        var input = payload ?? string.Empty;
        var functionContext = CreateContext(new NewtonsoftJsonObjectSerializer());
        var request = new MockHttpRequestData(functionContext, method: method,
            body: new MemoryStream(Encoding.UTF8.GetBytes(input)));
        request.Headers.Add("Content-Type", "application/json");
        if (token != null)
        {
            request.Headers.Add("Authorization", $"Bearer {token}");
        }
        return request;
    }

    public static FunctionContext CreateContext(ObjectSerializer serializer = null)
    {
        var context = new MockFunctionContext();

        var services = new ServiceCollection();
        services.AddOptions();
        services.AddFunctionsWorkerCore();

        services.Configure<WorkerOptions>(c =>
        {
            c.Serializer = serializer;
        });

        context.InstanceServices = services.BuildServiceProvider();

        return context;
    }

    public static MockTableService CreateMockTableService()
    {
        return new MockTableService();
    }

    public static FakeRealTimeSyncToAzureSqlService CreateFakeRealTimeSyncToAzureSqlService()
    {
        return new FakeRealTimeSyncToAzureSqlService();
    }

    public static SyncToAzureSql CreateSyncToAzureSql(IRealTimeSyncToAzureSqlService realTimeSyncToAzureSqlService)
    {
        return new SyncToAzureSql(realTimeSyncToAzureSqlService);
    }

    public static AzureSqlService CreateAzureSqlService(ISqlConnectionFactory sqlConnectionFactory,
        IQueryBuilderService queryBuilderService = null, ILogger<AzureSqlService> logger = null)
    {
        return new AzureSqlService(queryBuilderService ?? CreateQueryBuilderService(),
            sqlConnectionFactory, logger ?? A.Fake<ILogger<AzureSqlService>>());
    }

    public static CustomMetricsService CreateCustomMetricsService(TelemetryConfiguration telemetryConfiguration,
        ICurrentTimeService currentTimeService = null)
    {
        return new CustomMetricsService(telemetryConfiguration,
            currentTimeService ?? CreateCurrentTimeService());
    }
}

