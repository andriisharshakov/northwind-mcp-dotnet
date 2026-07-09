
zusing System.Text.Json;
using Microsoft.Extensions.Options;

namespace NorthwindCrm.Mcp.Auth;

/// <summary>
/// Obtains and caches an Auth0 M2M access token for the MCP Server using the
/// Client Credentials Grant (RFC 6749 §4.4). Auth0 tokens for this grant are
/// valid for 24h by default; this service refreshes a little before expiry
/// rather than waiting for a 401 from the API.
/// </summary>
public class Auth0TokenService
{
    private readonly HttpClient _http;
    private readonly Auth0ClientOptions _options;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private string? _cachedToken;
    private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

    // Refresh a bit early so an in-flight request never races a just-expired token.
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromMinutes(5);

    public Auth0TokenService(IHttpClientFactory httpClientFactory, IOptions<Auth0ClientOptions> options)
    {
        _http = httpClientFactory.CreateClient("auth0-token");
        _options = options.Value;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken ct = default)
    {
        if (_cachedToken is not null && DateTimeOffset.UtcNow < _expiresAt - RefreshSkew)
            return _cachedToken;

        await _lock.WaitAsync(ct);
        try
        {
            // Double-check after acquiring the lock — another caller may have refreshed already.
            if (_cachedToken is not null && DateTimeOffset.UtcNow < _expiresAt - RefreshSkew)
                return _cachedToken;

            var response = await _http.PostAsync(_options.TokenEndpoint, new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", _options.ClientId),
                new KeyValuePair<string, string>("client_secret", _options.ClientSecret),
                new KeyValuePair<string, string>("audience", _options.Audience),
            }), ct);

            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(ct);
            var payload = await JsonSerializer.DeserializeAsync<TokenResponse>(stream, cancellationToken: ct)
                ?? throw new InvalidOperationException("Auth0 token endpoint returned an empty response.");

            _cachedToken = payload.AccessToken;
            _expiresAt = DateTimeOffset.UtcNow.AddSeconds(payload.ExpiresIn);

            return _cachedToken;
        }
        finally
        {
            _lock.Release();
        }
    }

    private record TokenResponse(
        [property: System.Text.Json.Serialization.JsonPropertyName("access_token")] string AccessToken,
        [property: System.Text.Json.Serialization.JsonPropertyName("expires_in")] int ExpiresIn,
        [property: System.Text.Json.Serialization.JsonPropertyName("token_type")] string TokenType);
}
