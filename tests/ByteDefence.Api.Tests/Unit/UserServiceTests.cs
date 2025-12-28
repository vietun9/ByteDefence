using ByteDefence.Api.Data;
using ByteDefence.Api.Services;
using ByteDefence.Shared.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ByteDefence.Api.Tests.Unit;

public class UserServiceTests
{
    private readonly AppDbContext _context;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _userService = new UserService(_context);

        SeedData();
    }

    private void SeedData()
    {
        // Use BCrypt hashed passwords for tests
        var adminUser = new User
        {
            Id = "admin-test",
            Username = "admin",
            Email = "admin@test.com",
            PasswordHash = UserService.HashPassword("admin123"),
            Role = UserRole.Admin
        };

        var regularUser = new User
        {
            Id = "user-test",
            Username = "user",
            Email = "user@test.com",
            PasswordHash = UserService.HashPassword("user123"),
            Role = UserRole.User
        };

        _context.Users.AddRange(adminUser, regularUser);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsUser()
    {
        // Act
        var result = await _userService.GetByIdAsync("admin-test");

        // Assert
        result.Should().NotBeNull();
        result!.Username.Should().Be("admin");
        result.Role.Should().Be(UserRole.Admin);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Act
        var result = await _userService.GetByIdAsync("non-existent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByUsernameAsync_WithValidUsername_ReturnsUser()
    {
        // Act
        var result = await _userService.GetByUsernameAsync("user");

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("user@test.com");
    }

    [Fact]
    public async Task GetByUsernameAsync_WithInvalidUsername_ReturnsNull()
    {
        // Act
        var result = await _userService.GetByUsernameAsync("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateCredentialsAsync_WithValidCredentials_ReturnsUser()
    {
        // Act
        var result = await _userService.ValidateCredentialsAsync("admin", "admin123");

        // Assert
        result.Should().NotBeNull();
        result!.Username.Should().Be("admin");
    }

    [Fact]
    public async Task ValidateCredentialsAsync_WithInvalidPassword_ReturnsNull()
    {
        // Act
        var result = await _userService.ValidateCredentialsAsync("admin", "wrongpassword");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateCredentialsAsync_WithInvalidUsername_ReturnsNull()
    {
        // Act
        var result = await _userService.ValidateCredentialsAsync("wronguser", "admin123");

        // Assert
        result.Should().BeNull();
    }
}
