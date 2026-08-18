using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBizERP.Api.Data;
using SmartBizERP.Api.Domain;

namespace SmartBizERP.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/finance")]
public class FinanceController(AppDbContext db) : ControllerBase
{
    public record ExpenseRequest(string Category, string Description, decimal Amount, DateTime? ExpenseDate);

    [Authorize(Policy = "permission:expenses.manage")]
    [HttpGet("expenses")]
    public async Task<IActionResult> Expenses() =>
        Ok(await db.Expenses.OrderByDescending(x => x.ExpenseDate).ToListAsync());

    [Authorize(Policy = "permission:expenses.manage")]
    [HttpPost("expenses")]
    public async Task<IActionResult> CreateExpense(ExpenseRequest request)
    {
        if (request.Amount <= 0)
            return BadRequest(new { message = "Amount must be greater than zero." });

        var expense = new Expense
        {
            Category = request.Category.Trim(),
            Description = request.Description.Trim(),
            Amount = request.Amount,
            ExpenseDate = request.ExpenseDate?.ToUniversalTime() ?? DateTime.UtcNow
        };

        db.Expenses.Add(expense);
        await db.SaveChangesAsync();
        return Ok(expense);
    }

    [Authorize(Policy = "permission:reports.view")]
    [HttpGet("stock-movements")]
    public async Task<IActionResult> StockMovements() =>
        Ok(await db.StockMovements
            .Include(x => x.Product)
            .OrderByDescending(x => x.CreatedAt)
            .Take(200)
            .Select(x => new
            {
                x.Id,
                Product = x.Product.Name,
                x.QuantityChange,
                x.Type,
                x.ReferenceType,
                x.Note,
                x.CreatedAt
            })
            .ToListAsync());
}
