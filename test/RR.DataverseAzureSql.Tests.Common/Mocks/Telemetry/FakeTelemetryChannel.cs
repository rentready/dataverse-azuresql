using Microsoft.ApplicationInsights.Channel;

namespace RR.DataverseAzureSql.Tests.Common.Mocks.Telemetry;

public class FakeTelemetryChannel : ITelemetryChannel
{
    public FakeTelemetryChannel()
    {
        OnSend = telemetry => { };
        OnFlush = () => { };
        OnDispose = () => { };
    }

    public bool? DeveloperMode { get; set; }
    public string EndpointAddress { get; set; }
    public bool ThrowError { get; set; }
    public Action<ITelemetry> OnSend { get; set; }
    public Action OnFlush { get; set; }
    public Action OnDispose { get; set; }

    public void Send(ITelemetry item)
    {
        OnSend(item);
    }

    public void Dispose()
    {
        OnDispose();
        GC.SuppressFinalize(this);
    }

    public void Flush()
    {
        OnFlush();
    }
}

