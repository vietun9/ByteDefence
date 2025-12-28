using HotChocolate;

namespace ByteDefence.Api.GraphQL;

public class GraphQLErrorFilter : IErrorFilter
{
    public IError OnError(IError error)
    {
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
            
            _ => error
                .WithCode("INTERNAL_ERROR")
                .RemoveException()
        };
    }
}
