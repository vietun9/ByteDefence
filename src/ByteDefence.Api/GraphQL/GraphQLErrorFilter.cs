using HotChocolate;
using Microsoft.Extensions.Logging;

namespace ByteDefence.Api.GraphQL;

/// <summary>
/// Error filter that maps exceptions to structured GraphQL error codes.
/// Preserves debugging context in development while protecting sensitive info in production.
/// </summary>
public class GraphQLErrorFilter : IErrorFilter
{
    private readonly ILogger<GraphQLErrorFilter> _logger;
    private readonly bool _isDevelopment;

    public GraphQLErrorFilter(ILogger<GraphQLErrorFilter> logger, IHostEnvironment? environment = null)
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
            UnauthorizedAccessException => error
                .WithCode("UNAUTHORIZED")
                .WithMessage("You are not authorized to perform this action."),
            
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

// Minimal IHostEnvironment interface for DI (Azure Functions doesn't include this by default)
public interface IHostEnvironment
{
    string EnvironmentName { get; }
}
