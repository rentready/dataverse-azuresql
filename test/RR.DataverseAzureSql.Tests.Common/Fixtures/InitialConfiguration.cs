namespace RR.DataverseAzureSql.Tests.Common.Fixtures;

public class InitialConfiguration
{
    public static void SetEnvironmentVariables()
    {
        Environment.SetEnvironmentVariable("CrmOrganizationUrl", "https://some-fake.crm.dynamics.com/");

        Environment.SetEnvironmentVariable("VaultUrl", "https://some-fake.vault.azure.net/");
        Environment.SetEnvironmentVariable("FunctionAppClientScopes", "api://some-fake-scope/.default");

        Environment.SetEnvironmentVariable("LaunchDarklySdkKey", "some-fake-key");

        Environment.SetEnvironmentVariable("OneTimeSyncToAzureSqlServiceOptions:SynchronizedEntityLogicalNames", "some-fake-key");
        Environment.SetEnvironmentVariable("OneTimeSyncToAzureSqlServiceOptions:BatchSize", "2500");
        Environment.SetEnvironmentVariable("OneTimeSyncToAzureSqlServiceOptions:MaxActivityRetryCount", "3");
        Environment.SetEnvironmentVariable("OneTimeSyncToAzureSqlServiceOptions:ActivityRetryIntervalInSec", "30");

        Environment.SetEnvironmentVariable("RealTimeSyncToAzureSqlServiceOptions:MaxMessageRetryCount", "10");
        Environment.SetEnvironmentVariable("RealTimeSyncToAzureSqlServiceOptions:RelationshipEntityLogicalNames", "some-fake-key");

        Environment.SetEnvironmentVariable("AzureSqlServiceOptions:ConnectionString", "some-fake-connection-string");
        Environment.SetEnvironmentVariable("AzureSqlFullSyncServiceOptions:BatchSize", "2500");
        Environment.SetEnvironmentVariable("AzureSqlFullSyncServiceOptions:TimeoutInSec", "500");
        Environment.SetEnvironmentVariable("AzureSqlChangesSyncServiceOptions:InsertBatchSize", "1000");
        Environment.SetEnvironmentVariable("AzureSqlChangesSyncServiceOptions:UpdateBatchSize", "250");
        Environment.SetEnvironmentVariable("AzureSqlChangesSyncServiceOptions:DeleteBatchSize", "1000");

        Environment.SetEnvironmentVariable("DataverseAzureSqlServiceBusOptions:FullyQualifiedNamespace", "some-fake.servicebus.windows.net");
        Environment.SetEnvironmentVariable("DataverseAzureSqlServiceBusOptions:DefaultScheduledEnqueueTimeInMs", "6000");
        Environment.SetEnvironmentVariable("DataverseAzureSqlServiceBusTrigger__fullyQualifiedNamespace", "some-fake.servicebus.windows.net");

        Environment.SetEnvironmentVariable("TableServiceOptions:ConnectionString", "some-fake-connection-string");
        Environment.SetEnvironmentVariable("TableServiceOptions:TableName", "some-fake-table");
    }
}

