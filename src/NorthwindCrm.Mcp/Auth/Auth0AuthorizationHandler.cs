using System.Net.Http.Headers;

namespace NorthwindCrm.Mcp.Auth;

/// <summary>
/// Attaches "Authorization: Bearer {token}" to every request the MCP Server
/// makes to the Northwind API, using the cached M2M token from
/// <see cref="Auth0TokenService"/>. Registered on the "northwind-api" named
/// HttpClient so individual tool classes (CustomerTools, OrderTools, ...)
/// don't need to know anything about Auth0.
/// </summary>
public class Auth0AuthorizationHandler : DelegatingHandler
{
    private readonly Auth0TokenService _tokenService;

    public Auth0AuthorizationHandler(Auth0TokenService tokenService)
    {
        _tokenService = tokenService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _tokenService.GetAccessTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, cancellationToken);
    }
}
