using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBizERP.Api.Data;
using SmartBizERP.Api.Domain;

namespace SmartBizERP.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/parties")]
public class PartiesController(AppDbContext db) : ControllerBase
{
    public record PartyRequest(string Name, string? Phone, string? Email, string? Address);

    [Authorize(Policy = "permission:customers.manage")]
    [HttpGet("customers")]
    public async Task<IActionResult> Customers() =>
        Ok(await db.Customers.OrderByDescending(x => x.CreatedAt).ToListAsync());

    [Authorize(Policy = "permission:customers.manage")]
    [HttpPost("customers")]
    public async Task<IActionResult> CreateCustomer(PartyRequest request)
    {
        var item = new Customer
        {
            Name = request.Name.Trim(),
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address
        };
        db.Customers.Add(item);
        await db.SaveChangesAsync();
        return Ok(item);
    }

    [Authorize(Policy = "permission:suppliers.manage")]
    [HttpGet("suppliers")]
    public async Task<IActionResult> Suppliers() =>
        Ok(await db.Suppliers.OrderByDescending(x => x.CreatedAt).ToListAsync());

    [Authorize(Policy = "permission:suppliers.manage")]
    [HttpPost("suppliers")]
    public async Task<IActionResult> CreateSupplier(PartyRequest request)
    {
        var item = new Supplier
        {
            Name = request.Name.Trim(),
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address
        };
        db.Suppliers.Add(item);
        await db.SaveChangesAsync();
        return Ok(item);
    }
}
