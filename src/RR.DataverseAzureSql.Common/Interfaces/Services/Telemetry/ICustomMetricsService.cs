using RR.DataverseAzureSql.Common.Enums;

namespace RR.DataverseAzureSql.Common.Interfaces.Services.Telemetry;

public interface ICustomMetricsService
{
    void SyncFromDataverseOperationLatencyMs(string collection, string operation, DateTime timestamp);
    void TrackOrchestrationRuntimeStatus(SyncType syncType, int runtimeStatus);
}

