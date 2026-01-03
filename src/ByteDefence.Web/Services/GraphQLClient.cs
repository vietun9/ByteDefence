using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ByteDefence.Shared.Models;

namespace ByteDefence.Web.Services;

public interface IGraphQLClient
{
    Task<GraphQLResponse<T>> QueryAsync<T>(string query, object? variables = null);
    Task<GraphQLResponse<T>> MutateAsync<T>(string mutation, object? variables = null);
}

public class GraphQLClient : IGraphQLClient
{
    private readonly HttpClient _httpClient;
    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;
    private readonly JsonSerializerOptions _jsonOptions;

    public GraphQLClient(HttpClient httpClient, IAuthService authService, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _authService = authService;
        _configuration = configuration;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<GraphQLResponse<T>> QueryAsync<T>(string query, object? variables = null)
    {
        return await ExecuteAsync<T>(query, variables);
    }

    public async Task<GraphQLResponse<T>> MutateAsync<T>(string mutation, object? variables = null)
    {
        return await ExecuteAsync<T>(mutation, variables);
    }

    private async Task<GraphQLResponse<T>> ExecuteAsync<T>(string query, object? variables)
    {
        var request = new { query, variables };
        var content = new StringContent(
            JsonSerializer.Serialize(request, _jsonOptions),
            Encoding.UTF8,
            "application/json");

        var apiUrl = _configuration["Api:Url"] ?? "http://localhost:7071/api/graphql";
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, apiUrl)
        {
            Content = content
        };

        // Attach bearer token if available
        var token = await _authService.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        try
        {
            var response = await _httpClient.SendAsync(httpRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new GraphQLResponse<T>
                {
                    Errors = new[] { new GraphQLError { Message = $"HTTP Error: {response.StatusCode}" } }
                };
            }

            var result = JsonSerializer.Deserialize<GraphQLResponse<T>>(responseContent, _jsonOptions);
            return result ?? new GraphQLResponse<T>
            {
                Errors = new[] { new GraphQLError { Message = "Failed to deserialize response" } }
            };
        }
        catch (Exception ex)
        {
            return new GraphQLResponse<T>
            {
                Errors = new[] { new GraphQLError { Message = ex.Message } }
            };
        }
    }
}

public class GraphQLResponse<T>
{
    public T? Data { get; set; }
    public GraphQLError[]? Errors { get; set; }
    public bool HasErrors => Errors != null && Errors.Length > 0;
}

public class GraphQLError
{
    public string Message { get; set; } = string.Empty;
    public string? Code { get; set; }
    public GraphQLErrorExtensions? Extensions { get; set; }
}

public class GraphQLErrorExtensions
{
    public string? Code { get; set; }
}
