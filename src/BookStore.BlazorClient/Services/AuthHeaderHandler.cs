using System.Net.Http.Headers;

namespace BookStore.BlazorClient.Services;

public class AuthHeaderHandler : DelegatingHandler
{
    private readonly AuthTokenService _authTokenService;

    public AuthHeaderHandler(AuthTokenService authTokenService)
    {
        _authTokenService = authTokenService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = _authTokenService.Token;
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        return await base.SendAsync(request, cancellationToken);
    }
}
