namespace BookStore.BlazorClient.Services;

public class AuthTokenService
{
    private const string DefaultDemoToken = "demo-bearer-token-2024";
    
    private string _currentToken = DefaultDemoToken;

    public string Token => _currentToken;

    public void SetToken(string token)
    {
        _currentToken = token;
    }

    public void ResetToDefault()
    {
        _currentToken = DefaultDemoToken;
    }
}
