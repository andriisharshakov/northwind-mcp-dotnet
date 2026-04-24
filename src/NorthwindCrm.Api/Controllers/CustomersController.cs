using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NorthwindCrm.Api.Data;
using NorthwindCrm.Api.Models;

namespace NorthwindCrm.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly NorthwindDbContext _db;

    public CustomersController(NorthwindDbContext db)
    {
        _db = db;
    }

    // GET /api/customers
    // GET /api/customers?search=berlin
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int limit = 20)
    {
        var query = _db.Customers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c =>
                c.CompanyName.Contains(search) ||
                c.ContactName!.Contains(search) ||
                c.City!.Contains(search));

        var customers = await query
            .OrderBy(c => c.CompanyName)
            .Take(limit)
            .ToListAsync();

        return Ok(customers);
    }

    // GET /api/customers/ALFKI
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer is null) return NotFound();
        return Ok(customer);
    }

    // POST /api/customers
    [HttpPost]
    public async Task<IActionResult> Create(Customer customer)
    {
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = customer.CustomerId }, customer);
    }

    // PATCH /api/customers/{id}
    [HttpPatch("{id}")]
    public async Task<IActionResult> Update(string id, Customer updated)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer is null) return NotFound();

        customer.CompanyName = updated.CompanyName ?? customer.CompanyName;
        customer.ContactName = updated.ContactName ?? customer.ContactName;
        customer.City = updated.City ?? customer.City;
        customer.Country = updated.Country ?? customer.Country;
        customer.Phone = updated.Phone ?? customer.Phone;

        await _db.SaveChangesAsync();
        return Ok(customer);
    }

    // DELETE /api/customers/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer is null) return NotFound();

        _db.Customers.Remove(customer);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}