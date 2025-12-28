using ByteDefence.Api.Data;
using ByteDefence.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace ByteDefence.Api.Services;

public interface IUserService
{
    Task<User?> GetByIdAsync(string id);
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> ValidateCredentialsAsync(string username, string password);
}

public class UserService : IUserService
{
    private readonly AppDbContext _context;

    public UserService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(string id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<User?> ValidateCredentialsAsync(string username, string password)
    {
        var user = await GetByUsernameAsync(username);
        if (user == null) return null;

        // DEMO/MOCK IMPLEMENTATION: Plain text password comparison for demo purposes.
        // In production, use proper password hashing (BCrypt, Argon2, or ASP.NET Identity).
        // This approach is intentional for the open-book exercise as per requirements:
        // "Mock data, mock auth, mock error is acceptable."
        if (user.PasswordHash == password)
        {
            return user;
        }

        return null;
    }
}
