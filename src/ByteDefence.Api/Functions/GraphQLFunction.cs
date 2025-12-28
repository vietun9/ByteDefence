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

    public GraphQLFunction(
        IRequestExecutorResolver executorResolver,
        ILogger<GraphQLFunction> logger,
        IConfiguration configuration)
    {
        _executorResolver = executorResolver;
        _logger = logger;
        _configuration = configuration;
        _resultFormatter = new JsonResultFormatter();
    }

    [Function("graphql")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "graphql")] HttpRequestData req)
    {
        _logger.LogInformation("GraphQL request received");

        // Handle GET requests for GraphQL Playground/Banana Cake Pop
        if (req.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            return await HandlePlaygroundRequest(req);
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
            requestBuilder.SetVariableValues(request.Variables);
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

        // Serialize the result
        await using var stream = new MemoryStream();
        await _resultFormatter.FormatAsync(result, stream);
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var jsonResult = await reader.ReadToEndAsync();
        
        await response.WriteStringAsync(jsonResult);

        return response;
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

    private async Task<HttpResponseData> HandlePlaygroundRequest(HttpRequestData req)
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
}

public class GraphQLRequest
{
    public string Query { get; set; } = string.Empty;
    public string? OperationName { get; set; }
    public Dictionary<string, object?>? Variables { get; set; }
}
