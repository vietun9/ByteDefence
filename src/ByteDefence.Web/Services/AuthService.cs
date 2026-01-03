using Blazored.LocalStorage;

namespace ByteDefence.Web.Services;

public interface IAuthService
{
    Task<string?> GetTokenAsync();
    Task SetTokenAsync(string token);
    Task ClearTokenAsync();
    Task<bool> IsAuthenticatedAsync();
    event Action? OnAuthStateChanged;
}

public class AuthService : IAuthService
{
    private const string TokenKey = "auth_token";
    private readonly ILocalStorageService _localStorage;
    private string? _cachedToken;

    public event Action? OnAuthStateChanged;

    public AuthService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public async Task<string?> GetTokenAsync()
    {
        if (_cachedToken != null) return _cachedToken;
        
        try
        {
            _cachedToken = await _localStorage.GetItemAsync<string>(TokenKey);
            return _cachedToken;
        }
        catch
        {
            return null;
        }
    }

    public async Task SetTokenAsync(string token)
    {
        _cachedToken = token;
        await _localStorage.SetItemAsync(TokenKey, token);
        OnAuthStateChanged?.Invoke();
    }

    public async Task ClearTokenAsync()
    {
        _cachedToken = null;
        await _localStorage.RemoveItemAsync(TokenKey);
        OnAuthStateChanged?.Invoke();
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await GetTokenAsync();
        return !string.IsNullOrEmpty(token);
    }
}
