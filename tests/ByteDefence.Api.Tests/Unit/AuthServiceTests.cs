using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using ByteDefence.Api.Options;
using ByteDefence.Api.Services;
using ByteDefence.Shared.Models;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ByteDefence.Api.Tests.Unit;

public class AuthServiceTests
{
    private readonly AuthService _authService;
    private readonly JwtOptions _jwtOptions;

    public AuthServiceTests()
    {
        _jwtOptions = new JwtOptions
        {
            SigningKey = "Key-For-Development-Only-32Chars",
            Issuer = "ByteDefence-Test",
            Audience = "ByteDefence-Test-API",
            TokenLifetimeMinutes = 60
        };

        _authService = new AuthService(Microsoft.Extensions.Options.Options.Create(_jwtOptions));
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
    public void GenerateToken_ContainsExpectedClaims()
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
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        // Assert
        jwt.Issuer.Should().Be(_jwtOptions.Issuer);
        jwt.Audiences.Should().Contain(_jwtOptions.Audience);
        jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value.Should().Be(user.Id);
        jwt.Claims.First(c => c.Type == ClaimTypes.Role).Value.Should().Be(user.Role.ToString());
    }

    [Fact]
    public void GenerateToken_UsesConfiguredLifetime()
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
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var expiresInMinutes = (jwt.ValidTo - DateTime.UtcNow).TotalMinutes;

        // Assert
        expiresInMinutes.Should().BeApproximately(_jwtOptions.TokenLifetimeMinutes, 1);
    }
}
