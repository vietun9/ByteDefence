using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace ByteDefence.Api.Middleware;

/// <summary>
/// Middleware for JWT Bearer authentication in Azure Functions Worker.
/// Supports ASP.NET Core style JWT Bearer authentication with HotChocolate [Authorize].
/// 
/// Architecture options:
/// 1. Full validation: API validates JWT (default)
/// 2. APIM trust: APIM validates JWT, API trusts the token (skip validation)
/// 3. Gateway trust: App Service Gateway handles auth, API trusts headers
/// </summary>
public class JwtAuthenticationMiddleware : IFunctionsWorkerMiddleware
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtAuthenticationMiddleware> _logger;
    private readonly TokenValidationParameters? _validationParameters;
    private readonly bool _skipJwtValidation;

    public JwtAuthenticationMiddleware(
        IConfiguration configuration,
        ILogger<JwtAuthenticationMiddleware> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // Configuration option to skip JWT validation when APIM/Gateway handles it
        _skipJwtValidation = configuration.GetValue<bool>("Auth:SkipJwtValidation");

        if (!_skipJwtValidation)
        {
            var secret = configuration["Jwt:Secret"] ?? "ByteDefence-Super-Secret-Key-For-Development-Only-32Chars!";
            var issuer = configuration["Jwt:Issuer"] ?? "ByteDefence";
            var audience = configuration["Jwt:Audience"] ?? "ByteDefence-API";

            _validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        }
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var request = await context.GetHttpRequestDataAsync();

        if (request != null)
        {
            var principal = AuthenticateRequest(request);

            if (principal != null)
            {
                // Store the ClaimsPrincipal in FunctionContext for downstream use
                context.Items["ClaimsPrincipal"] = principal;

                // Extract user info for HotChocolate GlobalState
                var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                var role = principal.FindFirst(ClaimTypes.Role)?.Value;

                if (!string.IsNullOrEmpty(userId))
                {
                    context.Items["CurrentUser"] = userId;
                }

                if (!string.IsNullOrEmpty(role))
                {
                    context.Items["CurrentRole"] = role;
                }
            }
        }

        await next(context);
    }

    private ClaimsPrincipal? AuthenticateRequest(HttpRequestData request)
    {
        if (!request.Headers.TryGetValues("Authorization", out var authHeaders))
        {
            return null;
        }

        var authHeader = authHeaders.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var token = authHeader["Bearer ".Length..].Trim();

        if (_skipJwtValidation)
        {
            // Trust the token without validation (APIM/Gateway has validated it)
            return ParseTokenWithoutValidation(token);
        }

        return ValidateToken(token);
    }

    /// <summary>
    /// Parse token claims without cryptographic validation.
    /// Use only when APIM/Gateway has already validated the token.
    /// </summary>
    private ClaimsPrincipal? ParseTokenWithoutValidation(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            var claims = jwtToken.Claims.ToList();
            var identity = new ClaimsIdentity(claims, "Bearer", ClaimTypes.NameIdentifier, ClaimTypes.Role);
            return new ClaimsPrincipal(identity);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse JWT token (skip validation mode)");
            return null;
        }
    }

    /// <summary>
    /// Validate token with full cryptographic verification.
    /// </summary>
    private ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, _validationParameters!, out _);
            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return null;
        }
    }
}

/// <summary>
/// Extension methods for FunctionContext to work with authentication.
/// </summary>
public static class FunctionContextAuthExtensions
{
    /// <summary>
    /// Get the authenticated ClaimsPrincipal from the function context.
    /// </summary>
    public static ClaimsPrincipal? GetUser(this FunctionContext context)
    {
        if (context.Items.TryGetValue("ClaimsPrincipal", out var principal))
        {
            return principal as ClaimsPrincipal;
        }
        return null;
    }

    /// <summary>
    /// Get the current user ID from the function context.
    /// </summary>
    public static string? GetCurrentUserId(this FunctionContext context)
    {
        if (context.Items.TryGetValue("CurrentUser", out var userId))
        {
            return userId as string;
        }
        return null;
    }

    /// <summary>
    /// Get the current user role from the function context.
    /// </summary>
    public static string? GetCurrentRole(this FunctionContext context)
    {
        if (context.Items.TryGetValue("CurrentRole", out var role))
        {
            return role as string;
        }
        return null;
    }

    /// <summary>
    /// Check if the current user is authenticated.
    /// </summary>
    public static bool IsAuthenticated(this FunctionContext context)
    {
        return context.GetCurrentUserId() != null;
    }
}
