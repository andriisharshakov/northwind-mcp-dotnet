using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NorthwindCrm.Api.Data;
using NorthwindCrm.Api.Models;

namespace NorthwindCrm.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly NorthwindDbContext _db;

    public OrdersController(NorthwindDbContext db)
    {
        _db = db;
    }

    // GET /api/orders?customerId=ALFKI
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? customerId, [FromQuery] int limit = 20)
    {
        var query = _db.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(customerId))
            query = query.Where(o => o.CustomerId == customerId);

        var orders = await query
            .OrderByDescending(o => o.OrderDate)
            .Take(limit)
            .ToListAsync();

        return Ok(orders);
    }

    // GET /api/orders/10248
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var order = await _db.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .FirstOrDefaultAsync(o => o.OrderId == id);

        if (order is null) return NotFound();
        return Ok(order);
    }

    // POST /api/orders
    [HttpPost]
    public async Task<IActionResult> Create(Order order)
    {
        order.OrderDate = DateTime.UtcNow;
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = order.OrderId }, order);
    }

    // DELETE /api/orders/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order is null) return NotFound();
        _db.Orders.Remove(order);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}