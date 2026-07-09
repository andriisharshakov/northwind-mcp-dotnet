namespace NorthwindCrm.Mcp.Auth;

/// <summary>
/// Configuration for the MCP Server's own identity when calling the Northwind API.
/// This is a Machine-to-Machine (M2M) Auth0 application — the MCP Server
/// authenticates as itself, not as the end user (see docs/mcp-security.md,
/// "Option B: M2M service token").
/// </summary>
public class Auth0ClientOptions
{
    public const string SectionName = "Auth0";

    /// <summary>e.g. "dev-ldxrd4mx0aylxxy6.eu.auth0.com"</summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>API Identifier of the Northwind CRM API, e.g. "https://northwind-crm-api"</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>Client ID of the "Northwind MCP Server" M2M application.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Client Secret of the "Northwind MCP Server" M2M application.</summary>
    public string ClientSecret { get; set; } = string.Empty;

    public string TokenEndpoint => $"https://{Domain}/oauth/token";
}
