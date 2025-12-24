using Pulumi;
using Pulumi.AzureNative.Insights;
using Pulumi.AzureNative.Insights.Inputs;
using InsightsV20200202 = Pulumi.AzureNative.Insights.V20200202;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.ServiceBus;
using Config = Pulumi.Config;
using Provider = Pulumi.AzureNative.Provider;
using RR.DataverseAzureSql.Infra.Helpers;
using RR.PulumiExt.Constants.ResourceBuilders;
using RR.PulumiExt.ResourceBuilders.ServiceBus;
using RR.PulumiExt.ResourceBuilders.Storage;
using RR.SharedInfra.PulumiStack;
using RR.PulumiExt.Enums.ResourceBuilders.Insights;
using RR.PulumiExt.Enums.ResourceBuilders.WebApplication;
using RR.PulumiExt.ResourceBuilders.WebApplication;
using RR.PulumiExt.Mappers.ResourceOutputs.WebApplication;
using RR.PulumiExt.Models.ResourceOutputs.WebApplication;
using System.Collections.Immutable;
using RR.PulumiExt.Helpers.ResourceOutputs.Storage;
using RR.DataverseAzureSql.Common.Dtos.Configs;
using RR.DataverseAzureSql.Common.Constants;
using RR.PulumiExt.ResourceBuilders.Insights;

namespace RR.DataverseAzureSql.Infra;

public class ServiceStack : Stack
{
#if DEBUG
    private const string BuildConfiguration = "Debug";
#else
    private const string BuildConfiguration = "Release";
#endif

    private readonly string TableName = "azuresql";

    public ServiceStack() => CreateResources().Wait();

    private async Task CreateResources()
    {
        var stackName = $"{Pulumi.Deployment.Instance.StackName}";
        var serviceName = $"{Pulumi.Deployment.Instance.ProjectName}-{stackName}";
        var sharedServices = new SharedServices(stackName);

        var sharedResourcesStack = new StackReference("shared",
            new StackReferenceArgs { Name = $"organization/infra-shared/{stackName}" });
        var monitoringResourcesStack = new StackReference("monitoring",
            new StackReferenceArgs { Name = $"organization/monitoring/{stackName}" });

        var entities = FileHelpers.RetrieveEntityListFromFile();

        var config = new Config();

        var resourceGroup = new ResourceGroup(serviceName);

        var storageAccount = StorageAccountBuilder
            .Create(resourceGroup, StorageConstants.DefaultAccountName)
            .Build();

        _ = TableBuilder
            .Create(resourceGroup, storageAccount, TableName)
            .Build();

        var serviceBusNamespace = ServiceBusNamespaceBuilder
            .Create(resourceGroup, serviceName)
            .WithSharedServices(sharedServices)
            .Build();

        entities.Where(entity => !string.IsNullOrEmpty(entity.Id)).ToList()
            .ForEach(entity => CreateQueue(entity.Entity, resourceGroup, serviceBusNamespace));

        var storageAccountConnectionString = StorageAccountOutputHelpers.GetStorageAccountConnectionString(resourceGroup, storageAccount);
        var azureSqlConnectionString = sharedResourcesStack.RequireOutput("AzureSQLConnectionString").Apply(x => (string)x);

        var synchronizedEntities = string.Join(",",
            entities.Where(entity => !entity.Entity.Equals("all_relationship_entities"))
            .Select(entity => entity.Entity)
            .ToArray());

        var relationshipEntities = string.Join(",",
            entities.Where(entity => string.IsNullOrEmpty(entity.Id))
            .Select(entity => entity.Entity)
            .ToArray());

        var appInsights = ComponentBuilder
            .Create(resourceGroup, serviceName)
            .WithMonitoringServices(monitoringResourcesStack)
            .Build();

        var oneTimeFunctionAppProjectName = "RR.DataverseAzureSql.OneTime.Functions";
        var azureOneTimeFunctionsApp = await FunctionAppBuilder
            .Create(resourceGroup, $"{serviceName}-one-time", oneTimeFunctionAppProjectName)
            .WithBuildConfiguration(BuildConfiguration)
            .WithVersion(FunctionAppVersion.V4)
            .WithWorkerRuntime(FunctionAppWorkerRuntime.DotnetIsolated)
            .WithSharedServices(sharedServices)
            .WithMonitoringServices(monitoringResourcesStack)
            .WithStorageAccount(storageAccount)
            .WithAppInsights(appInsights)
            .AddServiceBusAccessSettings("DataverseAzureSql", serviceBusNamespace.FullyQualifiedNamespace, serviceBusNamespace.UserAssignedIdentityId)
            .AddAppSetting("TriggerTime", config.Require("one-time-sync-trigger-time"))
            .AddAppSetting("TableServiceOptions:ConnectionString", storageAccountConnectionString.Apply(x => x))
            .AddAppSetting("TableServiceOptions:TableName", TableName)
            .AddAppSetting("AzureSqlServiceOptions:ConnectionString", azureSqlConnectionString)
            .AddAppSetting("AzureSqlFullSyncServiceOptions:BatchSize", config.RequireInt32("azuresql-full-sync-batch-size").ToString())
            .AddAppSetting("AzureSqlFullSyncServiceOptions:TimeoutInSec", config.RequireInt32("azuresql-full-sync-timeout-in-sec").ToString())
            .AddAppSetting("AzureSqlChangesSyncServiceOptions:InsertBatchSize", config.RequireInt32("azuresql-changes-sync-insert-batch-size").ToString())
            .AddAppSetting("AzureSqlChangesSyncServiceOptions:UpdateBatchSize", config.RequireInt32("azuresql-changes-sync-update-batch-size").ToString())
            .AddAppSetting("AzureSqlChangesSyncServiceOptions:DeleteBatchSize", config.RequireInt32("azuresql-changes-sync-delete-batch-size").ToString())
            .AddAppSetting("OneTimeSyncToAzureSqlServiceOptions:SynchronizedEntityLogicalNames", synchronizedEntities)
            .AddAppSetting("OneTimeSyncToAzureSqlServiceOptions:MaxActivityRetryCount", config.RequireInt32("one-time-sync-max-activity-retry-count").ToString())
            .AddAppSetting("OneTimeSyncToAzureSqlServiceOptions:ActivityRetryIntervalInSec", config.RequireInt32("one-time-sync-activity-retry-interval-in-sec").ToString())
            .AddAppSetting("OneTimeSyncToAzureSqlServiceOptions:BatchSize", config.RequireInt32("one-time-sync-batch-size").ToString())
            .AddAppSetting("RealTimeSyncToAzureSqlServiceOptions:RelationshipEntityLogicalNames", relationshipEntities)
            .AddAppSetting("RealTimeSyncToAzureSqlServiceOptions:MaxMessageRetryCount", config.RequireInt32("real-time-sync-max-message-retry-count").ToString())
            .AddAppSetting("DataverseAzureSqlServiceBusOptions:DefaultScheduledEnqueueTimeInMs", config.RequireInt32("servicebus-scheduled-enqueue-time-in-ms").ToString())
            .AddAppSetting(ConfigSectionNames.RetrieveEntitySettings, RetrieveEntitySettingsFactory.GetDefaultSettings())
            .Build();

        var realTimeFunctionAppProjectName = "RR.DataverseAzureSql.RealTime.Functions";
        var azureRealTimeFunctionsApp = await FunctionAppBuilder
            .Create(resourceGroup, $"{serviceName}-real-time", realTimeFunctionAppProjectName)
            .WithBuildConfiguration(BuildConfiguration)
            .WithVersion(FunctionAppVersion.V4)
            .WithWorkerRuntime(FunctionAppWorkerRuntime.DotnetIsolated)
            .WithSharedServices(sharedServices)
            .WithMonitoringServices(monitoringResourcesStack)
            .WithStorageAccount(storageAccount)
            .WithAppInsights(appInsights)
            .AddServiceBusAccessSettings("DataverseAzureSql", serviceBusNamespace.FullyQualifiedNamespace, serviceBusNamespace.UserAssignedIdentityId)
            .AddAppSetting("TriggerTime", config.Require("one-time-sync-trigger-time"))
            .AddAppSetting("TableServiceOptions:ConnectionString", storageAccountConnectionString.Apply(x => x))
            .AddAppSetting("TableServiceOptions:TableName", TableName)
            .AddAppSetting("AzureSqlServiceOptions:ConnectionString", azureSqlConnectionString)
            .AddAppSetting("AzureSqlFullSyncServiceOptions:BatchSize", config.RequireInt32("azuresql-full-sync-batch-size").ToString())
            .AddAppSetting("AzureSqlFullSyncServiceOptions:TimeoutInSec", config.RequireInt32("azuresql-full-sync-timeout-in-sec").ToString())
            .AddAppSetting("AzureSqlChangesSyncServiceOptions:InsertBatchSize", config.RequireInt32("azuresql-changes-sync-insert-batch-size").ToString())
            .AddAppSetting("AzureSqlChangesSyncServiceOptions:UpdateBatchSize", config.RequireInt32("azuresql-changes-sync-update-batch-size").ToString())
            .AddAppSetting("AzureSqlChangesSyncServiceOptions:DeleteBatchSize", config.RequireInt32("azuresql-changes-sync-delete-batch-size").ToString())
            .AddAppSetting("OneTimeSyncToAzureSqlServiceOptions:SynchronizedEntityLogicalNames", synchronizedEntities)
            .AddAppSetting("OneTimeSyncToAzureSqlServiceOptions:MaxActivityRetryCount", config.RequireInt32("one-time-sync-max-activity-retry-count").ToString())
            .AddAppSetting("OneTimeSyncToAzureSqlServiceOptions:ActivityRetryIntervalInSec", config.RequireInt32("one-time-sync-activity-retry-interval-in-sec").ToString())
            .AddAppSetting("OneTimeSyncToAzureSqlServiceOptions:BatchSize", config.RequireInt32("one-time-sync-batch-size").ToString())
            .AddAppSetting("RealTimeSyncToAzureSqlServiceOptions:RelationshipEntityLogicalNames", relationshipEntities)
            .AddAppSetting("RealTimeSyncToAzureSqlServiceOptions:MaxMessageRetryCount", config.RequireInt32("real-time-sync-max-message-retry-count").ToString())
            .AddAppSetting("DataverseAzureSqlServiceBusOptions:DefaultScheduledEnqueueTimeInMs", config.RequireInt32("servicebus-scheduled-enqueue-time-in-ms").ToString())
            .AddAppSetting(ConfigSectionNames.RetrieveEntitySettings, RetrieveEntitySettingsFactory.GetDefaultSettings())
            .Build();

        CreateExtraMetricAlerts(resourceGroup, appInsights, sharedServices);

        var functionApps = new List<FunctionAppOutput>
            {
                new FunctionAppOutput
                {
                    Name = oneTimeFunctionAppProjectName,
                    AppId = azureOneTimeFunctionsApp.Id,
                    AppName = azureOneTimeFunctionsApp.Name,
                    AppUrl = azureOneTimeFunctionsApp.Url,
                    AppScope = azureOneTimeFunctionsApp.Scope,
                    AppUserAssignedIdentityId = azureOneTimeFunctionsApp.UserAssignedIdentityId,
                    AppAuthApplicationId = azureOneTimeFunctionsApp.AuthApplicationId,
                    AppDynamicsIdentityClientId = azureOneTimeFunctionsApp.DynamicsIdentityClientId
                },
                new FunctionAppOutput
                {
                    Name = realTimeFunctionAppProjectName,
                    AppId = azureRealTimeFunctionsApp.Id,
                    AppName = azureRealTimeFunctionsApp.Name,
                    AppUrl = azureRealTimeFunctionsApp.Url,
                    AppScope = azureRealTimeFunctionsApp.Scope,
                    AppUserAssignedIdentityId = azureRealTimeFunctionsApp.UserAssignedIdentityId,
                    AppAuthApplicationId = azureRealTimeFunctionsApp.AuthApplicationId,
                    AppDynamicsIdentityClientId = azureRealTimeFunctionsApp.DynamicsIdentityClientId
                }
            };

        FunctionAppNames = Output.Create(FunctionAppOutputMapper.Map(functionApps));
    }

    private static Queue CreateQueue(string name, ResourceGroup resourceGroup,
        Namespace serviceBusNamespace, Provider provider = null)
    {
        return ServiceBusQueueBuilder
            .Create(resourceGroup, serviceBusNamespace, name, provider)
            .WithBatchedOperations()
            .Build();
    }

    private static void CreateExtraMetricAlerts(ResourceGroup resourceGroup, InsightsV20200202.Component appInsights,
        SharedServices sharedServices, Provider provider = null)
    {
        var syncHourlyStatusMetricName = $"Replication {CustomMetricNames.HourlyStatusKey}";
        var syncHourlyStatusName = $"{syncHourlyStatusMetricName} is still running since previous invocation";

        var syncHourlyStatus = new MetricAlertSingleResourceMultipleMetricCriteriaArgs
        {
            AllOf = new InputList<MetricCriteriaArgs>
                {
                    new MetricCriteriaArgs
                    {
                        Name = syncHourlyStatusName,
                        MetricName = syncHourlyStatusMetricName,
                        MetricNamespace = "azure.applicationinsights",
                        Operator = Operator.LessThanOrEqual,
                        Threshold = 0,
                        TimeAggregation = AggregationTypeEnum.Maximum,
                        CriterionType = "StaticThresholdCriterion",
                        Dimensions = new InputList<MetricDimensionArgs>{}
                    }
                },
            OdataType = "Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria"
        };

        MetricAlertBuilder
            .Create(resourceGroup, appInsights, syncHourlyStatusName, provider)
            .WithCriteria(syncHourlyStatus)
            .WithSeverity(AlertSeverity.Critical)
            .WithEvaluationSettings("PT1H", "PT1H")
            .AddAction(new MetricAlertActionArgs { ActionGroupId = sharedServices.GetEngTeamActionGroup().Id })
            .Build();
    }

    [Output]
    public Output<ImmutableArray<ImmutableDictionary<string, object>>> FunctionAppNames { get; set; }
}

