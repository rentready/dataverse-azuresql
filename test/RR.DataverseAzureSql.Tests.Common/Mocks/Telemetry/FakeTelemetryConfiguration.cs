using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace RR.DataverseAzureSql.Tests.Common.Mocks.Telemetry;

public static class FakeTelemetryConfiguration
{
    public static TelemetryConfiguration Create(ICollection<ITelemetry> telemetryItems)
    {
        var telemetryConfiguration = new TelemetryConfiguration
        {
            ConnectionString = "InstrumentationKey=" + Guid.NewGuid().ToString(),
            TelemetryChannel = new FakeTelemetryChannel { OnSend = telemetryItems.Add }
        };

        return telemetryConfiguration;
    }
}

