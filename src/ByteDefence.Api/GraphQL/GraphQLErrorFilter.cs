using HotChocolate;
using Microsoft.Extensions.Logging;

namespace ByteDefence.Api.GraphQL;

/// <summary>
/// Error filter that maps exceptions to structured GraphQL error codes.
/// Preserves debugging context in development while protecting sensitive info in production.
/// Distinguishes between:
///   - UNAUTHENTICATED (401): User is not logged in
///   - FORBIDDEN (403): User is logged in but lacks permission
/// </summary>
public class GraphQLErrorFilter : IErrorFilter
{
    private readonly ILogger<GraphQLErrorFilter> _logger;
    private readonly bool _isDevelopment;

    public GraphQLErrorFilter(ILogger<GraphQLErrorFilter> logger, Microsoft.Extensions.Hosting.IHostEnvironment? environment = null)
    {
        _logger = logger;
        _isDevelopment = environment?.EnvironmentName == "Development";
    }

    public IError OnError(IError error)
    {
        // Log all errors for diagnostics
        _logger.LogError(error.Exception,
            "GraphQL Error: {Message}, Code: {Code}, Path: {Path}",
            error.Message,
            error.Code ?? "N/A",
            error.Path?.Print() ?? "N/A");

        // Map common exceptions to structured error codes
        return error.Exception switch
        {
            UnauthorizedAccessException ex => HandleUnauthorizedAccess(error, ex),

            InvalidOperationException => error
                .WithCode("INVALID_OPERATION")
                .WithMessage(error.Exception.Message),

            ArgumentException => error
                .WithCode("VALIDATION_ERROR")
                .WithMessage(error.Exception.Message),

            System.Collections.Generic.KeyNotFoundException => error
                .WithCode("NOT_FOUND")
                .WithMessage(error.Exception.Message),

            _ => CreateInternalError(error)
        };
    }

    /// <summary>
    /// Distinguishes between 401 (not authenticated) and 403 (forbidden).
    /// - "Authentication required" -> 401 UNAUTHENTICATED
    /// - "You can only..." or other permission messages -> 403 FORBIDDEN
    /// </summary>
    private static IError HandleUnauthorizedAccess(IError error, UnauthorizedAccessException ex)
    {
        var message = ex.Message;

        // Check if this is an authentication error (401) or authorization error (403)
        var isAuthenticationError = message.Contains("Authentication required", StringComparison.OrdinalIgnoreCase) ||
                                    message.Contains("not authenticated", StringComparison.OrdinalIgnoreCase);

        if (isAuthenticationError)
        {
            // 401 - User is not logged in
            return error
                .WithCode("UNAUTHENTICATED")
                .WithMessage("Authentication required. Please log in to access this resource.");
        }
        else
        {
            // 403 - User is logged in but doesn't have permission
            return error
                .WithCode("FORBIDDEN")
                .WithMessage(message);
        }
    }

    private IError CreateInternalError(IError error)
    {
        var builder = error
            .WithCode("INTERNAL_ERROR");

        if (_isDevelopment && error.Exception != null)
        {
            // In development, include exception details for debugging
            return builder
                .WithMessage($"{error.Message}: {error.Exception.Message}")
                .SetExtension("stackTrace", error.Exception.StackTrace);
        }

        // In production, hide internal details
        return builder
            .WithMessage("An unexpected error occurred. Please try again later.")
            .RemoveException();
    }
}

