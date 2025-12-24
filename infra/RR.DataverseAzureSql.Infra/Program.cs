using Pulumi;

namespace RR.DataverseAzureSql.Infra;

internal class Program
{
    static Task<int> Main() => Deployment.RunAsync<ServiceStack>();
}