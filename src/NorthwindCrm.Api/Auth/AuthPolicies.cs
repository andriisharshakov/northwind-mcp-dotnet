namespace NorthwindCrm.Api.Auth;

/// <summary>
/// Central place for every authorization policy name used across controllers.
/// Keeping these as constants avoids typo bugs in [Authorize(Policy = "...")] attributes.
/// </summary>
public static class AuthPolicies
{
    // Scope-based policies — one per OAuth2 scope defined on the Auth0 API.
    // These map 1:1 to what a token's "scope" claim can contain.
    public const string ReadCustomers = "read:customers";
    public const string WriteCustomers = "write:customers";
    public const string ReadOrders = "read:orders";
    public const string WriteOrders = "write:orders";
    public const string ReadProducts = "read:products";
    public const string WriteProducts = "write:products";

    // Role-based policies — coarser-grained, backed by the custom
    // "https://northwind-crm-api/roles" claim injected by the Auth0
    // Machine-to-Machine "Credentials Exchange" Action.
    public const string RequireReaderRole = "role:crm-reader";
    public const string RequireWriterRole = "role:crm-writer";
    public const string RequireAdminRole = "role:crm-admin";
}

/// <summary>
/// Role names as configured in Auth0 (User Management → Roles) and as emitted
/// into the custom roles claim by the Credentials Exchange Action.
/// </summary>
public static class AuthRoles
{
    public const string Reader = "crm-reader";
    public const string Writer = "crm-writer";
    public const string Admin = "crm-admin";

    /// <summary>The custom namespaced claim Auth0 Actions write roles into.</summary>
    public const string ClaimType = "https://northwind-crm-api/roles";
}
