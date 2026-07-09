namespace NorthwindCrm.Api.Auth;

/// <summary>
/// Strongly-typed binding for the "Auth0" section in appsettings.
/// Mirrors the values configured in the Auth0 dashboard for the
/// "Northwind CRM API" resource server.
/// </summary>
public class Auth0Options
{
    public const string SectionName = "Auth0";

    /// <summary>e.g. "dev-ldxrd4mx0aylxxy6.eu.auth0.com"</summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>API Identifier, e.g. "https://northwind-crm-api"</summary>
    public string Audience { get; set; } = string.Empty;

    public string Authority => $"https://{Domain}/";
}
