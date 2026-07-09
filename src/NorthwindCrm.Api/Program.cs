using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using NorthwindCrm.Api.Auth;
using NorthwindCrm.Api.Data;
using NorthwindCrm.Api.Middleware;
using NorthwindCrm.Api.Services;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

var auth0 = builder.Configuration.GetSection(Auth0Options.SectionName).Get<Auth0Options>()
    ?? throw new InvalidOperationException("Missing 'Auth0' configuration section.");

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuditLogService>();

// --- Authentication: validate JWTs issued by Auth0 (RS256, JWKS) ---
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = auth0.Authority;
        options.Audience = auth0.Audience;
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                // Auth0 M2M tokens put all scopes in a single space-separated
                // claim. ASP.NET Core policy engine does exact-match on claim
                // values, so we split the string into individual claims here.
                var identity = context.Principal?.Identity as ClaimsIdentity;
                if (identity is null) return Task.CompletedTask;

                var scopeClaim = identity.FindFirst("scope");
                if (scopeClaim is null) return Task.CompletedTask;

                var scopes = scopeClaim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (scopes.Length <= 1) return Task.CompletedTask; // already a single claim or empty

                identity.RemoveClaim(scopeClaim);
                foreach (var scope in scopes)
                    identity.AddClaim(new Claim("scope", scope));

                return Task.CompletedTask;
            }
        };
    });

// --- Authorization: scope-based policies (one per OAuth2 scope) + role-based policies ---
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AuthPolicies.ReadCustomers, p => p.RequireClaim("scope", AuthPolicies.ReadCustomers))
    .AddPolicy(AuthPolicies.WriteCustomers, p => p.RequireClaim("scope", AuthPolicies.WriteCustomers))
    .AddPolicy(AuthPolicies.ReadOrders, p => p.RequireClaim("scope", AuthPolicies.ReadOrders))
    .AddPolicy(AuthPolicies.WriteOrders, p => p.RequireClaim("scope", AuthPolicies.WriteOrders))
    .AddPolicy(AuthPolicies.ReadProducts, p => p.RequireClaim("scope", AuthPolicies.ReadProducts))
    .AddPolicy(AuthPolicies.WriteProducts, p => p.RequireClaim("scope", AuthPolicies.WriteProducts))
    .AddPolicy(AuthPolicies.RequireReaderRole, p => p.RequireClaim(AuthRoles.ClaimType,
        AuthRoles.Reader, AuthRoles.Writer, AuthRoles.Admin)) // writer/admin imply reader
    .AddPolicy(AuthPolicies.RequireWriterRole, p => p.RequireClaim(AuthRoles.ClaimType,
        AuthRoles.Writer, AuthRoles.Admin))
    .AddPolicy(AuthPolicies.RequireAdminRole, p => p.RequireClaim(AuthRoles.ClaimType,
        AuthRoles.Admin));

// Note: ASP.NET Core splits Auth0's space-separated "scope" string into
// individual claim values automatically via the default JwtBearer claim
// mapping, so RequireClaim("scope", "read:orders") works against a token
// with scope: "read:orders write:orders".

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            ClientCredentials = new OpenApiOAuthFlow
            {
                TokenUrl = new Uri($"{auth0.Authority}oauth/token"),
                Scopes = new Dictionary<string, string>
                {
                    [AuthPolicies.ReadCustomers] = "Read customers",
                    [AuthPolicies.WriteCustomers] = "Write customers",
                    [AuthPolicies.ReadOrders] = "Read orders",
                    [AuthPolicies.WriteOrders] = "Write orders",
                    [AuthPolicies.ReadProducts] = "Read products",
                    [AuthPolicies.WriteProducts] = "Write products",
                }
            }
        }
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddDbContext<NorthwindDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    // "Northwind Test Client" SPA — used only for interactively obtaining a
    // token in Swagger UI (Authorization Code + PKCE). The actual MCP Server
    // never goes through this flow; it uses Client Credentials Grant directly
    // (see NorthwindCrm.Mcp/Auth/Auth0TokenService.cs).
    options.OAuthClientId(builder.Configuration["Auth0:SwaggerClientId"]);
    options.OAuthUsePkce();
});
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
