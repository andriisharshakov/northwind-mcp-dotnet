using ModelContextProtocol.Server;
using System.ComponentModel;

namespace NorthwindCrm.Mcp.Tools;

[McpServerToolType]
public class ProductTools
{
    private readonly HttpClient _http;

    public ProductTools(HttpClient http)
    {
        _http = http;
    }

    [McpServerTool, Description("Search products by name. Returns only active (non-discontinued) products by default.")]
    public async Task<string> SearchProducts(
        [Description("Product name to search for")] string query,
        [Description("Maximum number of results")] int limit = 10)
    {
        var response = await _http.GetAsync($"/api/products?search={query}&limit={limit}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    [McpServerTool, Description("Get a specific product by its numeric ID")]
    public async Task<string> GetProduct(
        [Description("The numeric product ID")] int productId)
    {
        var response = await _http.GetAsync($"/api/products/{productId}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return $"Product '{productId}' not found.";
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    [McpServerTool, Description("Update the unit price of a product. Price must be greater than zero.")]
    public async Task<string> UpdateProductPrice(
        [Description("The numeric product ID")] int productId,
        [Description("The new unit price (must be > 0)")] decimal newPrice)
    {
        if (newPrice <= 0)
            return "Error: price must be greater than zero.";

        var response = await _http.PatchAsJsonAsync($"/api/products/{productId}/price", newPrice);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return $"Product '{productId}' not found.";
        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            return "Error: invalid price value.";
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}