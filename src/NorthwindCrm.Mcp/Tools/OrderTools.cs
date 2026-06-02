using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace NorthwindCrm.Mcp.Tools;

[McpServerToolType]
public class OrderTools
{
    private readonly HttpClient _http;

    public OrderTools(HttpClient http)
    {
        _http = http;
    }

    [McpServerTool, Description("Get a specific order by ID including customer info, line items, and products")]
    public async Task<string> GetOrder(
        [Description("The numeric order ID (e.g. 10248)")] int orderId)
    {
        var response = await _http.GetAsync($"/api/orders/{orderId}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return $"Order '{orderId}' not found.";
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    [McpServerTool, Description("Get recent orders, optionally filtered by customer ID")]
    public async Task<string> GetOrders(
        [Description("Optional customer ID to filter by (e.g. ALFKI)")] string? customerId = null,
        [Description("Maximum number of orders to return")] int limit = 5)
    {
        var url = customerId is not null
            ? $"/api/orders?customerId={customerId}&limit={limit}"
            : $"/api/orders?limit={limit}";

        var response = await _http.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    [McpServerTool, Description("Create a new order for a customer")]
    public async Task<string> CreateOrder(
        [Description("The 5-character customer ID (e.g. ALFKI)")] string customerId,
        [Description("Employee ID to assign the order to")] int employeeId = 1,
        [Description("Shipping city")] string? shipCity = null,
        [Description("Shipping country")] string? shipCountry = null)
    {
        var order = new
        {
            customerId,
            employeeId,
            shipCity,
            shipCountry,
            orderDate = DateTime.UtcNow
        };

        var response = await _http.PostAsJsonAsync("/api/orders", order);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return $"Customer '{customerId}' not found.";
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}