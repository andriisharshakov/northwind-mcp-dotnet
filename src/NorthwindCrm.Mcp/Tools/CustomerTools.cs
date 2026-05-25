using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace NorthwindCrm.Mcp.Tools;

[McpServerToolType]
public class CustomerTools
{
    private readonly HttpClient _http;

    public CustomerTools(HttpClient http)
    {
        _http = http;
    }

    [McpServerTool, Description("Search Northwind customers by company name, contact name, or city")]
    public async Task<string> SearchCustomers(
        [Description("Company name, contact person, or city to search for")] string query,
        [Description("Maximum number of results to return")] int limit = 10)
    {
        var response = await _http.GetAsync($"/api/customers?search={query}&limit={limit}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    [McpServerTool, Description("Get a specific customer by their ID (e.g. ALFKI)")]
    public async Task<string> GetCustomer(
        [Description("The 5-character customer ID")] string customerId)
    {
        var response = await _http.GetAsync($"/api/customers/{customerId}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return $"Customer '{customerId}' not found.";
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    [McpServerTool, Description("Get all orders for a specific customer including order details and products")]
    public async Task<string> GetCustomerOrders(
        [Description("The 5-character customer ID")] string customerId,
        [Description("Maximum number of orders to return")] int limit = 5)
    {
        var response = await _http.GetAsync($"/api/orders?customerId={customerId}&limit={limit}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}