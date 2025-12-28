using ByteDefence.Api.Services;
using ByteDefence.Shared.DTOs;

namespace ByteDefence.Api.GraphQL.Schema.Mutations;

[ExtendObjectType("Mutation")]
public class AuthMutationResolver
{
    /// <summary>
    /// Authenticate a user and receive a JWT token.
    /// </summary>
    public async Task<LoginPayload> Login(
        LoginInput input,
        [Service] IUserService userService,
        [Service] IAuthService authService)
    {
        if (string.IsNullOrWhiteSpace(input.Username))
        {
            return new LoginPayload(null, null, "Username is required");
        }

        if (string.IsNullOrWhiteSpace(input.Password))
        {
            return new LoginPayload(null, null, "Password is required");
        }

        var user = await userService.ValidateCredentialsAsync(input.Username, input.Password);
        if (user == null)
        {
            return new LoginPayload(null, null, "Invalid username or password");
        }

        var token = authService.GenerateToken(user);
        return new LoginPayload(token, user);
    }
}
