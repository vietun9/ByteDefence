using System.Security.Claims;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using HotChocolate.Execution;
using HotChocolate.Execution.Serialization;
using System.Net;
using System.Text.Json;
using ByteDefence.Api.Middleware;

namespace ByteDefence.Api.Functions;

public class GraphQLFunction(
    IRequestExecutorResolver executorResolver,
    ILogger<GraphQLFunction> logger)
{
    private readonly IRequestExecutorResolver _executorResolver = executorResolver;
    private readonly ILogger<GraphQLFunction> _logger = logger;
    private readonly JsonResultFormatter _resultFormatter = new();

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Function("graphql")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "options", Route = "graphql")] HttpRequestData req,
        FunctionContext context)
    {
        _logger.LogInformation("GraphQL request received");

        // Handle GET requests for GraphQL Playground/Banana Cake Pop
        if (req.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            return await HandlePlaygroundRequest(req);
        }

        // Parse the GraphQL request
        var body = await new StreamReader(req.Body).ReadToEndAsync();
        var request = JsonSerializer.Deserialize<GraphQLRequest>(body, _jsonOptions);

        if (request == null || string.IsNullOrEmpty(request.Query))
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(new { errors = new[] { new { message = "Invalid GraphQL request" } } });
            return badResponse;
        }

        // Get user info from JWT Authentication Middleware
        var userId = context.GetCurrentUserId();
        var userRole = context.GetCurrentRole();
        var claimsPrincipal = context.GetUser();

        // Get the executor
        var executor = await _executorResolver.GetRequestExecutorAsync();

        // Build the request using OperationRequestBuilder
        var requestBuilder = OperationRequestBuilder.New()
            .SetDocument(request.Query);

        if (!string.IsNullOrEmpty(request.OperationName))
        {
            requestBuilder.SetOperationName(request.OperationName);
        }

        if (request.Variables != null)
        {
            requestBuilder.SetVariableValues(NormalizeVariables(request.Variables));
        }

        // Set user context for authorization (HotChocolate GlobalState)
        if (!string.IsNullOrEmpty(userId))
        {
            requestBuilder.SetGlobalState("CurrentUser", userId);
            if (!string.IsNullOrEmpty(userRole))
            {
                requestBuilder.SetGlobalState("CurrentRole", userRole);
            }
        }

        // Set ClaimsPrincipal for HotChocolate [Authorize] attribute
        if (claimsPrincipal != null)
        {
            requestBuilder.SetGlobalState("ClaimsPrincipal", claimsPrincipal);
        }

        // Execute the request
        var result = await executor.ExecuteAsync(requestBuilder.Build());

        // Create the response
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");

        // Serialize the result
        await using var stream = new MemoryStream();
        await _resultFormatter.FormatAsync(result, stream);
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var jsonResult = await reader.ReadToEndAsync();

        await response.WriteStringAsync(jsonResult);

        return response;
    }

    private static async Task<HttpResponseData> HandlePlaygroundRequest(HttpRequestData req)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");

        var info = new
        {
            service = "ByteDefence GraphQL API",
            version = "1.0.0",
            endpoint = "POST /api/graphql",
            documentation = "Use a GraphQL client (Postman, GraphiQL, Banana Cake Pop) to query this endpoint.",
            testCredentials = new
            {
                admin = new { username = "admin", password = "admin123" },
                user = new { username = "user", password = "user123" }
            },
            exampleQuery = "mutation { login(input: { username: \"admin\", password: \"admin123\" }) { token user { id username role } } }"
        };

        await response.WriteAsJsonAsync(info);
        return response;
    }

    private static Dictionary<string, object?> NormalizeVariables(Dictionary<string, object?> variables)
    {
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var kvp in variables)
        {
            result[kvp.Key] = ConvertValue(kvp.Value);
        }
        return result;
    }

    private static object? ConvertValue(object? value)
    {
        if (value is null) return null;

        if (value is JsonElement je) return ConvertJsonElement(je);

        if (value is Dictionary<string, object?> dict)
        {
            var converted = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var kv in dict)
            {
                converted[kv.Key] = ConvertValue(kv.Value);
            }
            return converted;
        }

        if (value is IEnumerable<object?> list)
        {
            return list.Select(ConvertValue).ToList();
        }

        return value;
    }

    private static object? ConvertJsonElement(JsonElement je)
    {
        switch (je.ValueKind)
        {
            case JsonValueKind.Null: return null;
            case JsonValueKind.String: return je.GetString();
            case JsonValueKind.True: return true;
            case JsonValueKind.False: return false;
            case JsonValueKind.Number:
                if (je.TryGetInt64(out var l)) return l;
                if (je.TryGetDecimal(out var d)) return d;
                return je.GetDouble();
            case JsonValueKind.Object:
                {
                    var obj = new Dictionary<string, object?>(StringComparer.Ordinal);
                    foreach (var prop in je.EnumerateObject())
                    {
                        obj[prop.Name] = ConvertJsonElement(prop.Value);
                    }
                    return obj;
                }
            case JsonValueKind.Array:
                {
                    var list = new List<object?>();
                    foreach (var item in je.EnumerateArray())
                    {
                        list.Add(ConvertJsonElement(item));
                    }
                    return list;
                }
            default: return je.ToString();
        }
    }
}

public class GraphQLRequest
{
    public string Query { get; set; } = string.Empty;
    public string? OperationName { get; set; }
    public Dictionary<string, object?>? Variables { get; set; }
}
