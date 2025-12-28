using ByteDefence.Api.Services;
using ByteDefence.Shared.Models;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ByteDefence.Api.Tests.Unit;

public class AuthServiceTests
{
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Jwt:Secret", "ByteDefence-Super-Secret-Key-For-Development-Only-32Chars!" },
                { "Jwt:Issuer", "ByteDefence-Test" },
                { "Jwt:Audience", "ByteDefence-Test-API" }
            })
            .Build();

        _authService = new AuthService(configuration);
    }

    [Fact]
    public void GenerateToken_ReturnsValidToken()
    {
        // Arrange
        var user = new User
        {
            Id = "test-user-id",
            Username = "testuser",
            Email = "test@test.com",
            Role = UserRole.Admin
        };

        // Act
        var token = _authService.GenerateToken(user);

        // Assert
        token.Should().NotBeNullOrEmpty();
        token.Should().Contain("."); // JWT should have parts separated by dots
    }

    [Fact]
    public void ValidateToken_WithValidToken_ReturnsPrincipal()
    {
        // Arrange
        var user = new User
        {
            Id = "test-user-id",
            Username = "testuser",
            Email = "test@test.com",
            Role = UserRole.User
        };
        var token = _authService.GenerateToken(user);

        // Act
        var principal = _authService.ValidateToken(token);

        // Assert
        principal.Should().NotBeNull();
    }

    [Fact]
    public void ValidateToken_WithInvalidToken_ReturnsNull()
    {
        // Act
        var principal = _authService.ValidateToken("invalid-token");

        // Assert
        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_WithTamperedToken_ReturnsNull()
    {
        // Arrange
        var user = new User
        {
            Id = "test-user-id",
            Username = "testuser",
            Email = "test@test.com",
            Role = UserRole.User
        };
        var token = _authService.GenerateToken(user);
        var tamperedToken = token + "tampered";

        // Act
        var principal = _authService.ValidateToken(tamperedToken);

        // Assert
        principal.Should().BeNull();
    }
}
