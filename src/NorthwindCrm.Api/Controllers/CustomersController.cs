using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NorthwindCrm.Api.Auth;
using NorthwindCrm.Api.Data;
using NorthwindCrm.Api.Models;
using NorthwindCrm.Api.Services;

namespace NorthwindCrm.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // require a valid JWT for every action in this controller; specific
            // policies below narrow it further per scope
public class CustomersController : ControllerBase
{
    private readonly NorthwindDbContext _db;
    private readonly AuditLogService _audit;

    public CustomersController(NorthwindDbContext db, AuditLogService audit)
    {
        _db = db;
        _audit = audit;
    }

    // GET /api/customers
    // GET /api/customers?search=berlin
    [HttpGet]
    [Authorize(Policy = AuthPolicies.ReadCustomers)]
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
    [Authorize(Policy = AuthPolicies.ReadCustomers)]
    public async Task<IActionResult> GetById(string id)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer is null) return NotFound();
        return Ok(customer);
    }

    // POST /api/customers
    [HttpPost]
    [Authorize(Policy = AuthPolicies.WriteCustomers)]
    public async Task<IActionResult> Create(Customer customer)
    {
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("customer", customer.CustomerId, "INSERT", newValues: customer);
        return CreatedAtAction(nameof(GetById), new { id = customer.CustomerId }, customer);
    }

    // PATCH /api/customers/{id}
    [HttpPatch("{id}")]
    [Authorize(Policy = AuthPolicies.WriteCustomers)]
    public async Task<IActionResult> Update(string id, Customer updated)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer is null) return NotFound();

        var oldValues = new { customer.CompanyName, customer.ContactName, customer.City };

        customer.CompanyName = updated.CompanyName ?? customer.CompanyName;
        customer.ContactName = updated.ContactName ?? customer.ContactName;
        customer.City = updated.City ?? customer.City;
        customer.Country = updated.Country ?? customer.Country;
        customer.Phone = updated.Phone ?? customer.Phone;

        await _db.SaveChangesAsync();
        await _audit.LogAsync("customer", id, "UPDATE", oldValues: oldValues, newValues: customer);
        return Ok(customer);
    }

    // DELETE /api/customers/{id}
    [HttpDelete("{id}")]
    [Authorize(Policy = AuthPolicies.WriteCustomers)]
    public async Task<IActionResult> Delete(string id)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer is null) return NotFound();

        _db.Customers.Remove(customer);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("customer", id, "DELETE", oldValues: customer);
        return NoContent();
    }
}
