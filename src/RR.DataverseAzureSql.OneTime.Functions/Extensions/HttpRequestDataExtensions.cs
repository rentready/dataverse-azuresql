using Microsoft.Azure.Functions.Worker.Http;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace RR.DataverseAzureSql.OneTime.Functions.Extensions;

public static class HttpRequestDataExtensions
{
    public static Task<HttpResponseData> CreateOk<T>(this HttpRequestData request, T obj)
    {
        return request.GetReponse(obj, HttpStatusCode.OK);
    }

    public static Task<HttpResponseData> CreateOk(this HttpRequestData request)
    {
        object obj = null;

        return request.GetReponse(obj, HttpStatusCode.OK);
    }

    public static Task<HttpResponseData> CreateBadRequest<T>(this HttpRequestData request, T obj)
    {
        return request.GetReponse(obj, HttpStatusCode.BadRequest);
    }

    public static async Task<HttpResponseData> ToBadRequest(this HttpRequestData request, IEnumerable<ValidationResult> validationResults)
    {
        var msg = $"Model is invalid: {string.Join(", ", validationResults.Select(s => s.ErrorMessage).ToArray())}";

        return await request.CreateBadRequest(msg);
    }

    public static async Task<HttpResponseData> GetReponse<T>(this HttpRequestData request,
        T obj, HttpStatusCode statusCode)
    {
        var response = request.CreateResponse();
        if (obj is not null)
        {
            await response.WriteAsJsonAsync(obj);
        }
        response.StatusCode = statusCode;

        return response;
    }
}

