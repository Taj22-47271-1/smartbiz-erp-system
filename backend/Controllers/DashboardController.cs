using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBizERP.Api.Data;

namespace SmartBizERP.Api.Controllers;

[Authorize(Policy = "permission:dashboard.view")]
[ApiController]
[Route("api/dashboard")]
public class DashboardController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var now = DateTime.UtcNow;
        var from = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-5);

        var sales = await db.Sales
            .Where(x => x.CreatedAt >= from)
            .Select(x => new { x.TotalAmount, x.CreatedAt })
            .ToListAsync();

        var purchases = await db.Purchases
            .Where(x => x.CreatedAt >= from)
            .Select(x => new { x.TotalAmount, x.CreatedAt })
            .ToListAsync();

        var saleItems = await db.SaleItems
            .Include(x => x.Sale)
            .Where(x => x.Sale.CreatedAt >= from)
            .Select(x => new { x.Quantity, x.UnitCost })
            .ToListAsync();

        var expenses = await db.Expenses
            .Where(x => x.ExpenseDate >= from)
            .Select(x => new { x.Amount, x.ExpenseDate })
            .ToListAsync();

        var totalSales = sales.Sum(x => x.TotalAmount);
        var totalPurchases = purchases.Sum(x => x.TotalAmount);
        var cogs = saleItems.Sum(x => x.UnitCost * x.Quantity);
        var totalExpenses = expenses.Sum(x => x.Amount);
        var profit = totalSales - cogs - totalExpenses;

        var months = Enumerable.Range(0, 6)
            .Select(offset => from.AddMonths(offset))
            .Select(month => new
            {
                Key = month.ToString("yyyy-MM"),
                Label = month.ToString("MMM yyyy"),
                Start = month,
                End = month.AddMonths(1)
            })
            .ToArray();

        var trend = months.Select(m => new
        {
            month = m.Label,
            sales = sales.Where(x => x.CreatedAt >= m.Start && x.CreatedAt < m.End).Sum(x => x.TotalAmount),
            purchases = purchases.Where(x => x.CreatedAt >= m.Start && x.CreatedAt < m.End).Sum(x => x.TotalAmount)
        });

        var lowStock = await db.Products
            .Where(x => x.CurrentStock <= x.ReorderLevel)
            .OrderBy(x => x.CurrentStock)
            .Take(8)
            .Select(x => new { x.Id, x.Name, x.Sku, x.CurrentStock, x.ReorderLevel })
            .ToListAsync();

        var recentSales = await db.Sales
            .Include(x => x.Customer)
            .OrderByDescending(x => x.CreatedAt)
            .Take(6)
            .Select(x => new
            {
                x.Id,
                x.InvoiceNo,
                Customer = x.Customer.Name,
                x.TotalAmount,
                x.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            kpis = new
            {
                totalSales,
                totalPurchases,
                totalExpenses,
                profit,
                totalProducts = await db.Products.CountAsync(),
                totalCustomers = await db.Customers.CountAsync()
            },
            trend,
            lowStock,
            recentSales
        });
    }
}
