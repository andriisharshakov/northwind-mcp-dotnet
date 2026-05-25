using NorthwindCrm.Mcp.Tools;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<CustomerTools>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] 
        ?? "http://localhost:5185");
});

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<CustomerTools>();

var app = builder.Build();

app.MapMcp();

app.Run();