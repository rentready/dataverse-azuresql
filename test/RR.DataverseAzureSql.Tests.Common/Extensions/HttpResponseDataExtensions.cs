using Microsoft.Azure.Functions.Worker.Http;
using Newtonsoft.Json;
using System.IO;

namespace RR.DataverseAzureSql.Tests.Common.Extensions
{
    public static class HttpResponseDataExtensions
    {
        public static T ReadFromBody<T>(this HttpResponseData response)
        {
            response.Body.Position = 0;
            using var sr = new StreamReader(response.Body);
            using var jr = new JsonTextReader(sr);
            var serializer = new JsonSerializer();
            return serializer.Deserialize<T>(jr);
        }

        public static string ReadStringFromBody(this HttpResponseData response)
        {
            response.Body.Position = 0;
            using var sr = new StreamReader(response.Body);
            return sr.ReadToEnd();
        }
    }
}
