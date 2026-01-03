using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ByteDefence.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;

namespace ByteDefence.Web.Auth;

public class CustomAuthStateProvider : AuthenticationStateProvider, IDisposable
{
    private readonly IAuthService _authService;
    private ClaimsPrincipal _anonymous = new(new ClaimsIdentity());

    public CustomAuthStateProvider(IAuthService authService)
    {
        _authService = authService;
        _authService.OnAuthStateChanged += NotifyAuthenticationStateChanged;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _authService.GetTokenAsync();
        
        if (string.IsNullOrEmpty(token))
        {
            return new AuthenticationState(_anonymous);
        }

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            
            // Check if token is expired
            if (jwtToken.ValidTo < DateTime.UtcNow)
            {
                await _authService.ClearTokenAsync();
                return new AuthenticationState(_anonymous);
            }

            var claims = jwtToken.Claims.ToList();
            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);

            return new AuthenticationState(user);
        }
        catch
        {
            await _authService.ClearTokenAsync();
            return new AuthenticationState(_anonymous);
        }
    }

    private void NotifyAuthenticationStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public void Dispose()
    {
        _authService.OnAuthStateChanged -= NotifyAuthenticationStateChanged;
    }
}
