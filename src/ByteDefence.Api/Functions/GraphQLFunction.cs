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
        response.Headers.Add("Content-Type", "text/html");

        var html = @"
<!DOCTYPE html>
<html>
<head>
    <title>ByteDefence GraphQL</title>
    <style>
        body { margin: 0; padding: 40px; font-family: system-ui, -apple-system, sans-serif; background: #1a1a2e; color: #fff; }
        h1 { margin-bottom: 20px; }
        .info { background: #16213e; padding: 20px; border-radius: 8px; margin-bottom: 20px; }
        code { background: #0f3460; padding: 2px 6px; border-radius: 4px; }
        a { color: #e94560; }
    </style>
</head>
<body>
    <h1>🚀 ByteDefence GraphQL API</h1>
    <div class='info'>
        <p>This is the GraphQL endpoint. Use a GraphQL client like:</p>
        <ul>
            <li><a href='https://www.postman.com/' target='_blank'>Postman</a></li>
            <li><a href='https://github.com/graphql/graphiql' target='_blank'>GraphiQL</a></li>
            <li><a href='https://chillicream.com/products/bananacakepop' target='_blank'>Banana Cake Pop</a></li>
        </ul>
        <p>Endpoint: <code>POST /api/graphql</code></p>
        <h3>Test Credentials</h3>
        <table style='border-collapse: collapse;'>
            <tr><td style='padding: 5px; border: 1px solid #333;'>Admin</td><td style='padding: 5px; border: 1px solid #333;'>admin / admin123</td></tr>
            <tr><td style='padding: 5px; border: 1px solid #333;'>User</td><td style='padding: 5px; border: 1px solid #333;'>user / user123</td></tr>
        </table>
        <h3>Example Login Mutation</h3>
        <pre style='background: #0f3460; padding: 10px; border-radius: 4px; overflow-x: auto;'>
mutation {
  login(input: { username: ""admin"", password: ""admin123"" }) {
    token
    user { id username role }
    errorMessage
  }
}
        </pre>
    </div>
</body>
</html>";

        await response.WriteStringAsync(html);
        return response;
    }
}

public class GraphQLRequest
{
    public string Query { get; set; } = string.Empty;
    public string? OperationName { get; set; }
    public Dictionary<string, object?>? Variables { get; set; }
}
