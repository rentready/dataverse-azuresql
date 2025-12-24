using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.DurableTask.Client;
using RR.DataverseAzureSql.Common.Constants;
using RR.DataverseAzureSql.Common.Enums;
using RR.DataverseAzureSql.Services.Services.Telemetry;
using RR.DataverseAzureSql.Tests.Common.Factories;
using RR.DataverseAzureSql.Tests.Common.Mocks.Telemetry;
using Xunit;

namespace RR.DataverseAzureSql.Services.UnitTests.Services.Telemetry;

public class CustomMetricsServiceTests
{
    private readonly TelemetryConfiguration _fakeTelemetryConfiguration;
    private readonly CustomMetricsService _customMetricsService;

    private readonly List<ITelemetry> _telemetryItems = new List<ITelemetry>();

    public CustomMetricsServiceTests()
    {
        _fakeTelemetryConfiguration = FakeTelemetryConfiguration.Create(_telemetryItems);
        _customMetricsService = EntityFactory.CreateCustomMetricsService(_fakeTelemetryConfiguration);
    }

    [Fact]
    public void SyncFromDataverseOperationLatencyMs_ShouldCallTrackLatencyMetric_WithCorrectParameters()
    {
        // Act
        _customMetricsService.SyncFromDataverseOperationLatencyMs(EntityLogicalNames.Account,
            ReplicationOperationType.Upsert.ToString(), DateTime.UtcNow.AddSeconds(1));
        var metricTelemetry = (MetricTelemetry)_telemetryItems.FirstOrDefault();

        // Assert
        Assert.Single(_telemetryItems);
        Assert.Equal(4, metricTelemetry.Properties.Count);
        Assert.True(metricTelemetry.Sum < 1);
    }

    [Fact]
    public void TrackOrchestrationRuntimeStatus_ShouldTrackMetricsWithCorrectParameters()
    {
        // Act
        _customMetricsService.TrackOrchestrationRuntimeStatus(SyncType.Changes,
            (int)OrchestrationRuntimeStatus.Running);
        var metricTelemetry = (MetricTelemetry)_telemetryItems.FirstOrDefault();

        // Assert
        Assert.Single(_telemetryItems);
        Assert.Equal(2, metricTelemetry.Properties.Count);
        Assert.Equal(0, metricTelemetry.Sum);
    }
}

