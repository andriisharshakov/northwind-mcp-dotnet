using NorthwindCrm.Mcp.Auth;
using NorthwindCrm.Mcp.Tools;

var builder = WebApplication.CreateBuilder(args);

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5185";

builder.Services.Configure<Auth0ClientOptions>(
    builder.Configuration.GetSection(Auth0ClientOptions.SectionName));

// Plain HttpClient used by Auth0TokenService to call the /oauth/token endpoint.
// Deliberately NOT routed through Auth0AuthorizationHandler — fetching a token
// must not itself require a token.
builder.Services.AddHttpClient("auth0-token");

builder.Services.AddSingleton<Auth0TokenService>();
builder.Services.AddTransient<Auth0AuthorizationHandler>();

// Every call to the Northwind API automatically carries a Bearer token for
// the "Northwind MCP Server" M2M application (see docs/mcp-security.md,
// "Option B: M2M service token").
builder.Services.AddHttpClient("northwind-api", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
}).AddHttpMessageHandler<Auth0AuthorizationHandler>();

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<CustomerTools>()
    .WithTools<ProductTools>()
    .WithTools<OrderTools>();

builder.Services.AddSingleton(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    return factory.CreateClient("northwind-api");
});

var app = builder.Build();

app.MapMcp();

app.Run();
