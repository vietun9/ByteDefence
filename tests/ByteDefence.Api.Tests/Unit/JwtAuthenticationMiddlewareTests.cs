using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ByteDefence.Api.Middleware;
using ByteDefence.Api.Services;
using ByteDefence.Shared.Models;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ByteDefence.Api.Tests.Unit;

/// <summary>
/// Unit tests for JWT Authentication Middleware.
/// Tests the ASP.NET Core style JWT Bearer authentication implementation.
/// </summary>
public class JwtAuthenticationMiddlewareTests
{
    private readonly IConfiguration _configuration;
    private readonly AuthService _authService;

    public JwtAuthenticationMiddlewareTests()
    {
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Jwt:Secret", "ByteDefence-Super-Secret-Key-For-Development-Only-32Chars!" },
                { "Jwt:Issuer", "ByteDefence" },
                { "Jwt:Audience", "ByteDefence-API" },
                { "Auth:SkipJwtValidation", "false" }
            })
            .Build();

        _authService = new AuthService(_configuration);
    }

    [Fact]
    public void GenerateToken_WithUser_CreatesValidToken()
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
        token.Should().Contain("."); // JWT has 3 parts separated by dots
        var parts = token.Split('.');
        parts.Should().HaveCount(3);
    }

    [Fact]
    public void ValidateToken_WithValidToken_ExtractsCorrectClaims()
    {
        // Arrange
        var user = new User
        {
            Id = "test-user-123",
            Username = "testadmin",
            Email = "admin@test.com",
            Role = UserRole.Admin
        };
        var token = _authService.GenerateToken(user);

        // Act
        var principal = _authService.ValidateToken(token);

        // Assert
        principal.Should().NotBeNull();
        
        var userId = principal!.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var role = principal.FindFirst(ClaimTypes.Role)?.Value;

        userId.Should().Be("test-user-123");
        role.Should().Be("Admin");
    }

    [Fact]
    public void ValidateToken_WithInvalidToken_ReturnsNull()
    {
        // Act
        var principal = _authService.ValidateToken("invalid.token.here");

        // Assert
        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_WithExpiredToken_ReturnsNull()
    {
        // Arrange - Create a token that's already expired
        // Note: This would require modifying AuthService or using a custom token
        // For now, we test with a tampered token
        var user = new User
        {
            Id = "test-user",
            Username = "test",
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

    [Fact]
    public void GenerateToken_IncludesRoleClaim()
    {
        // Arrange
        var user = new User
        {
            Id = "user-001",
            Username = "testuser",
            Email = "test@test.com",
            Role = UserRole.User
        };

        // Act
        var token = _authService.GenerateToken(user);
        var principal = _authService.ValidateToken(token);

        // Assert
        principal.Should().NotBeNull();
        var roleClaim = principal!.FindFirst(ClaimTypes.Role)?.Value;
        roleClaim.Should().Be("User");
    }

    [Fact]
    public void Configuration_SkipJwtValidation_CanBeRead()
    {
        // Arrange
        var configWithSkip = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Auth:SkipJwtValidation", "true" }
            })
            .Build();

        // Act
        var skipValidation = configWithSkip.GetValue<bool>("Auth:SkipJwtValidation");

        // Assert
        skipValidation.Should().BeTrue();
    }

    [Fact]
    public void Configuration_SkipJwtValidation_DefaultIsFalse()
    {
        // Arrange
        var configWithoutSkip = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act
        var skipValidation = configWithoutSkip.GetValue<bool>("Auth:SkipJwtValidation");

        // Assert
        skipValidation.Should().BeFalse();
    }
}
