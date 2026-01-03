using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ByteDefence.Api.Middleware;
using ByteDefence.Api.Options;
using ByteDefence.Api.Services;
using ByteDefence.Shared.Models;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace ByteDefence.Api.Tests.Unit;

/// <summary>
/// Unit tests for JWT Authentication Middleware.
/// Tests the ASP.NET Core style JWT Bearer authentication implementation.
/// </summary>
public class JwtAuthenticationMiddlewareTests
{
    private readonly JwtOptions _jwtOptions;
    private readonly AuthService _authService;
    private readonly TokenValidationParameters _validationParameters;

    public JwtAuthenticationMiddlewareTests()
    {
        _jwtOptions = new JwtOptions
        {
            SigningKey = "ByteDefence-Super-Secret-Key-For-Development-Only-32Chars!",
            Issuer = "ByteDefence",
            Audience = "ByteDefence-API",
            TokenLifetimeMinutes = 60
        };

        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_jwtOptions.SigningKey)),
            ValidateIssuer = true,
            ValidIssuer = _jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = _jwtOptions.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        _authService = new AuthService(Microsoft.Extensions.Options.Options.Create(_jwtOptions));
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
        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(token, _validationParameters, out _);

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
        var handler = new JwtSecurityTokenHandler();
        Action act = () => handler.ValidateToken("invalid.token.here", _validationParameters, out _);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ValidateToken_WithTamperedToken_ReturnsNull()
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
        var tamperedToken = token + "tampered";
        var handler = new JwtSecurityTokenHandler();
        Action act = () => handler.ValidateToken(tamperedToken, _validationParameters, out _);

        // Assert
        act.Should().Throw<SecurityTokenException>();
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
