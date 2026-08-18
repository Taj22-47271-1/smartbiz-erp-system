using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBizERP.Api.Data;
using SmartBizERP.Api.Domain;

namespace SmartBizERP.Api.Controllers;

[Authorize(Policy = "permission:purchases.manage")]
[ApiController]
[Route("api/purchases")]
public class PurchasesController(AppDbContext db) : ControllerBase
{
    public record PurchaseLine(Guid ProductId, int Quantity, decimal UnitCost);
    public record CreatePurchaseRequest(Guid SupplierId, List<PurchaseLine> Items);

    [HttpGet]
    public async Task<IActionResult> List() =>
        Ok(await db.Purchases
            .Include(x => x.Supplier)
            .Include(x => x.Items)
            .ThenInclude(x => x.Product)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.PurchaseNo,
                Supplier = x.Supplier.Name,
                x.TotalAmount,
                x.Status,
                x.CreatedAt,
                Items = x.Items.Select(i => new
                {
                    i.ProductId,
                    Product = i.Product.Name,
                    i.Quantity,
                    i.UnitCost,
                    i.LineTotal
                })
            })
            .ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create(CreatePurchaseRequest request)
    {
        if (request.Items.Count == 0)
            return BadRequest(new { message = "At least one purchase item is required." });

        if (!await db.Suppliers.AnyAsync(x => x.Id == request.SupplierId))
            return BadRequest(new { message = "Supplier not found." });

        var productIds = request.Items.Select(x => x.ProductId).Distinct().ToArray();
        var products = await db.Products.Where(x => productIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id);

        if (products.Count != productIds.Length)
            return BadRequest(new { message = "One or more products were not found." });

        await using var tx = await db.Database.BeginTransactionAsync();

        var purchase = new Purchase
        {
            PurchaseNo = $"PUR-{DateTime.UtcNow:yyyyMMddHHmmss}",
            SupplierId = request.SupplierId,
            Status = "Received"
        };

        foreach (var line in request.Items)
        {
            if (line.Quantity <= 0 || line.UnitCost < 0)
                return BadRequest(new { message = "Quantity and unit cost are invalid." });

            var product = products[line.ProductId];
            var item = new PurchaseItem
            {
                ProductId = product.Id,
                Quantity = line.Quantity,
                UnitCost = line.UnitCost,
                LineTotal = line.Quantity * line.UnitCost
            };

            purchase.Items.Add(item);
            purchase.TotalAmount += item.LineTotal;

            product.CurrentStock += line.Quantity;
            product.PurchasePrice = line.UnitCost;
            product.UpdatedAt = DateTime.UtcNow;

            db.StockMovements.Add(new StockMovement
            {
                ProductId = product.Id,
                QuantityChange = line.Quantity,
                Type = "Purchase",
                ReferenceType = "Purchase",
                ReferenceId = purchase.Id,
                Note = purchase.PurchaseNo
            });
        }

        db.Purchases.Add(purchase);
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return Ok(new { purchase.Id, purchase.PurchaseNo, purchase.TotalAmount });
    }
}
