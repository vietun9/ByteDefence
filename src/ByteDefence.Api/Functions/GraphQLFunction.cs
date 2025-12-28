using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using HotChocolate.Execution;
using HotChocolate.Execution.Serialization;
using System.Net;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;

namespace ByteDefence.Api.Functions;

public class GraphQLFunction
{
    private readonly IRequestExecutorResolver _executorResolver;
    private readonly ILogger<GraphQLFunction> _logger;
    private readonly IConfiguration _configuration;
    private readonly JsonResultFormatter _resultFormatter;
    private readonly string[] _allowedOrigins;

    public GraphQLFunction(
        IRequestExecutorResolver executorResolver,
        ILogger<GraphQLFunction> logger,
        IConfiguration configuration)
    {
        _executorResolver = executorResolver;
        _logger = logger;
        _configuration = configuration;
        _resultFormatter = new JsonResultFormatter();
        
        // Load allowed origins from configuration (default to localhost for dev)
        _allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
            ?? new[] { "http://localhost:5001", "http://localhost:5173", "https://localhost:5001" };
    }

    [Function("graphql")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "options", Route = "graphql")] HttpRequestData req)
    {
        _logger.LogInformation("GraphQL request received");

        // Get the request origin for CORS
        var origin = GetRequestOrigin(req);

        // Short-circuit CORS preflight
        if (req.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
        {
            var preflight = req.CreateResponse(HttpStatusCode.OK);
            AddCorsHeaders(preflight, origin);
            return preflight;
        }

        // Handle GET requests for GraphQL Playground/Banana Cake Pop
        if (req.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            return await HandlePlaygroundRequest(req, origin);
        }

        // Parse the GraphQL request
        var body = await new StreamReader(req.Body).ReadToEndAsync();
        var request = JsonSerializer.Deserialize<GraphQLRequest>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (request == null || string.IsNullOrEmpty(request.Query))
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            AddCorsHeaders(badResponse, origin);
            await badResponse.WriteAsJsonAsync(new { errors = new[] { new { message = "Invalid GraphQL request" } } });
            return badResponse;
        }

        // Extract and validate JWT token if present
        var userId = ExtractUserIdFromToken(req);

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
            var normalized = NormalizeVariables(request.Variables);
            requestBuilder.SetVariableValues(normalized);
        }

        // Set user context for authorization
        if (!string.IsNullOrEmpty(userId))
        {
            requestBuilder.SetGlobalState("CurrentUser", userId);
        }

        // Execute the request
        var result = await executor.ExecuteAsync(requestBuilder.Build());

        // Create the response
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
        AddCorsHeaders(response, origin);

        // Serialize the result
        await using var stream = new MemoryStream();
        await _resultFormatter.FormatAsync(result, stream);
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var jsonResult = await reader.ReadToEndAsync();
        
        await response.WriteStringAsync(jsonResult);

        return response;
    }

    private string? GetRequestOrigin(HttpRequestData req)
    {
        if (req.Headers.TryGetValues("Origin", out var origins))
        {
            return origins.FirstOrDefault();
        }
        return null;
    }

    private string? ExtractUserIdFromToken(HttpRequestData req)
    {
        if (!req.Headers.TryGetValues("Authorization", out var authHeaders))
            return null;

        var authHeader = authHeaders.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;

        var token = authHeader["Bearer ".Length..].Trim();

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var secret = _configuration["Jwt:Secret"] ?? "ByteDefence-Super-Secret-Key-For-Development-Only-32Chars!";
            var key = Encoding.UTF8.GetBytes(secret);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"] ?? "ByteDefence",
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"] ?? "ByteDefence-API",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return null;
        }
    }

    private async Task<HttpResponseData> HandlePlaygroundRequest(HttpRequestData req, string? origin)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
        AddCorsHeaders(response, origin);

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

    private void AddCorsHeaders(HttpResponseData response, string? requestOrigin)
    {
        // Use specific origin if it's in the allowed list, otherwise use first allowed origin
        string corsOrigin;
        if (!string.IsNullOrEmpty(requestOrigin) && _allowedOrigins.Contains(requestOrigin, StringComparer.OrdinalIgnoreCase))
        {
            corsOrigin = requestOrigin;
        }
        else
        {
            corsOrigin = _allowedOrigins.FirstOrDefault() ?? "http://localhost:5001";
        }

        response.Headers.Add("Access-Control-Allow-Origin", corsOrigin);
        response.Headers.Add("Access-Control-Allow-Headers", "Content-Type,Authorization");
        response.Headers.Add("Access-Control-Allow-Methods", "GET,POST,OPTIONS");
        response.Headers.Add("Access-Control-Allow-Credentials", "true");
    }

    private static IReadOnlyDictionary<string, object?> NormalizeVariables(Dictionary<string, object?> variables)
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
        if (value is null)
            return null;

        if (value is JsonElement je)
        {
            return ConvertJsonElement(je);
        }

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
            case JsonValueKind.Null:
                return null;
            case JsonValueKind.String:
                return je.GetString();
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
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
            default:
                return je.ToString();
        }
    }
}

public class GraphQLRequest
{
    public string Query { get; set; } = string.Empty;
    public string? OperationName { get; set; }
    public Dictionary<string, object?>? Variables { get; set; }
}
