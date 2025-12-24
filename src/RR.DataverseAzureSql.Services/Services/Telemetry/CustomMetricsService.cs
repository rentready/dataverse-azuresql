using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using RR.Common.Interfaces;
using RR.Common.Validations;
using RR.DataverseAzureSql.Common.Constants;
using RR.DataverseAzureSql.Common.Enums;
using RR.DataverseAzureSql.Common.Interfaces.Services.Telemetry;

namespace RR.DataverseAzureSql.Services.Services.Telemetry;

public class CustomMetricsService : ICustomMetricsService
{
    private readonly TelemetryClient _telemetryClient;
    private readonly ICurrentTimeService _currentTimeService;

    public CustomMetricsService(TelemetryConfiguration telemetryConfiguration,
        ICurrentTimeService currentTimeService)
    {
        _telemetryClient = new TelemetryClient(telemetryConfiguration);
        _currentTimeService = currentTimeService.IsNotNull(nameof(currentTimeService));
    }

    public void SyncFromDataverseOperationLatencyMs(string collection, string operation, DateTime timestamp)
    {
        TrackLatencyMetric(ReplicationType.DataverseToAzureSql, collection, operation, GetLatencyMs(timestamp));
    }

    public void TrackOrchestrationRuntimeStatus(SyncType syncType, int runtimeStatus)
    {
        var properties = new Dictionary<string, string>()
            {
                { "SyncType", syncType.ToString() },
                { "Category", "Host.Aggregator" }
            };

        _telemetryClient.TrackMetric($"Replication {CustomMetricNames.HourlyStatusKey}", runtimeStatus, properties);
    }

    private void TrackLatencyMetric(ReplicationType replicationType, string collection, string operation, double delayMs)
    {
        var properties = new Dictionary<string, string>()
        {
            { "ReplicationType", replicationType.ToString() },
            { "CollectionName", collection },
            { "OperationType", operation },
            { "Category", "Host.Aggregator" }
        };

        _telemetryClient.TrackMetric($"Replication {CustomMetricNames.LatencyKey}", delayMs, properties);
    }

    private double GetLatencyMs(DateTime timestamp)
    {
        return (_currentTimeService.GetCurrentUTCTime() - timestamp).TotalMilliseconds;
    }
}

