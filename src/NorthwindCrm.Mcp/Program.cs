using NorthwindCrm.Mcp.Tools;

var builder = WebApplication.CreateBuilder(args);

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5185";

builder.Services.AddHttpClient("northwind-api", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<CustomerTools>();

builder.Services.AddSingleton(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    return factory.CreateClient("northwind-api");
});

var app = builder.Build();

app.MapMcp();

app.Run();