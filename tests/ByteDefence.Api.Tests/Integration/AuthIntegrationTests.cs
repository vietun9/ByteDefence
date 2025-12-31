using FluentAssertions;
using HotChocolate.Execution;
using Xunit;

namespace ByteDefence.Api.Tests.Integration;

/// <summary>
/// Integration tests for the Login mutation.
/// Tests the complete authentication flow through GraphQL.
/// </summary>
public class AuthIntegrationTests : GraphQLIntegrationTestBase
{
    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokenAndUser()
    {
        // Arrange
        var query = @"
            mutation Login($input: LoginInput!) {
                login(input: $input) {
                    token
                    user {
                        id
                        username
                        email
                        role
                    }
                    errorMessage
                }
            }";

        var variables = new Dictionary<string, object?>
        {
            ["input"] = new Dictionary<string, object?>
            {
                ["username"] = "admin",
                ["password"] = "admin123"
            }
        };

        // Act
        var result = await ExecuteAsync(query, variables);

        // Assert
        HasErrors(result).Should().BeFalse();
        var data = GetData(result);
        data.Should().NotBeNull();
        
        var login = data!["login"] as IReadOnlyDictionary<string, object?>;
        login.Should().NotBeNull();
        login!["token"].Should().NotBeNull();
        login["errorMessage"].Should().BeNull();
        
        var user = login["user"] as IReadOnlyDictionary<string, object?>;
        user.Should().NotBeNull();
        user!["username"]!.ToString().Should().Be("admin");
        user["role"]!.ToString().Should().Be("ADMIN");
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsError()
    {
        // Arrange
        var query = @"
            mutation Login($input: LoginInput!) {
                login(input: $input) {
                    token
                    errorMessage
                }
            }";

        var variables = new Dictionary<string, object?>
        {
            ["input"] = new Dictionary<string, object?>
            {
                ["username"] = "admin",
                ["password"] = "wrongpassword"
            }
        };

        // Act
        var result = await ExecuteAsync(query, variables);

        // Assert
        HasErrors(result).Should().BeFalse();
        var data = GetData(result);
        var login = data!["login"] as IReadOnlyDictionary<string, object?>;
        
        login!["token"].Should().BeNull();
        login["errorMessage"]!.ToString().Should().Be("Invalid username or password");
    }

    [Fact]
    public async Task Login_WithEmptyUsername_ReturnsError()
    {
        // Arrange
        var query = @"
            mutation Login($input: LoginInput!) {
                login(input: $input) {
                    token
                    errorMessage
                }
            }";

        var variables = new Dictionary<string, object?>
        {
            ["input"] = new Dictionary<string, object?>
            {
                ["username"] = "",
                ["password"] = "admin123"
            }
        };

        // Act
        var result = await ExecuteAsync(query, variables);

        // Assert
        HasErrors(result).Should().BeFalse();
        var data = GetData(result);
        var login = data!["login"] as IReadOnlyDictionary<string, object?>;
        
        login!["token"].Should().BeNull();
        login["errorMessage"]!.ToString().Should().Be("Username is required");
    }

    [Fact]
    public async Task Login_GeneratedTokenIsValid()
    {
        // Arrange
        var query = @"
            mutation Login($input: LoginInput!) {
                login(input: $input) {
                    token
                    user { id }
                }
            }";

        var variables = new Dictionary<string, object?>
        {
            ["input"] = new Dictionary<string, object?>
            {
                ["username"] = "admin",
                ["password"] = "admin123"
            }
        };

        // Act
        var result = await ExecuteAsync(query, variables);
        var data = GetData(result);
        var login = data!["login"] as IReadOnlyDictionary<string, object?>;
        var token = login!["token"]!.ToString();

        // Assert - validate the token using AuthService
        var principal = AuthService.ValidateToken(token!);
        principal.Should().NotBeNull();
    }
}
