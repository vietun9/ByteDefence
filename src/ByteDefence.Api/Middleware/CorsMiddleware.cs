using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using System.Net;

namespace ByteDefence.Api.Middleware;

public class CorsMiddleware(IConfiguration configuration) : IFunctionsWorkerMiddleware
{
    private readonly string[] _allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? ["http://localhost:8080", "http://localhost:5001"];

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var request = await context.GetHttpRequestDataAsync();

        if (request?.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase) == true)
        {
            var response = request.CreateResponse(HttpStatusCode.OK);
            AddCorsHeaders(request, response);
            context.GetInvocationResult().Value = response;
            return;
        }

        await next(context);

        var httpResponse = context.GetHttpResponseData();
        if (httpResponse != null && request != null)
        {
            AddCorsHeaders(request, httpResponse);
        }
    }

    private void AddCorsHeaders(HttpRequestData request, HttpResponseData response)
    {
        var origin = request.Headers.TryGetValues("Origin", out var values) ? values.FirstOrDefault() : null;

        if (!string.IsNullOrEmpty(origin) && _allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
        {
            response.Headers.TryAddWithoutValidation("Access-Control-Allow-Origin", origin);
        }
        else
        {
            response.Headers.TryAddWithoutValidation("Access-Control-Allow-Origin", _allowedOrigins.FirstOrDefault() ?? "http://localhost:8080");
        }

        response.Headers.TryAddWithoutValidation("Access-Control-Allow-Headers", "Content-Type,Authorization");
        response.Headers.TryAddWithoutValidation("Access-Control-Allow-Methods", "GET,POST,OPTIONS");
        response.Headers.TryAddWithoutValidation("Access-Control-Allow-Credentials", "true");
        response.Headers.TryAddWithoutValidation("Access-Control-Max-Age", "86400");
    }
}
