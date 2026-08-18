using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBizERP.Api.Data;
using SmartBizERP.Api.Domain;

namespace SmartBizERP.Api.Controllers;

[Authorize(Policy = "permission:sales.manage")]
[ApiController]
[Route("api/sales")]
public class SalesController(AppDbContext db) : ControllerBase
{
    public record SaleLine(Guid ProductId, int Quantity, decimal UnitPrice);
    public record CreateSaleRequest(Guid CustomerId, decimal Discount, List<SaleLine> Items);

    [HttpGet]
    public async Task<IActionResult> List() =>
        Ok(await db.Sales
            .Include(x => x.Customer)
            .Include(x => x.Items)
            .ThenInclude(x => x.Product)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.InvoiceNo,
                Customer = x.Customer.Name,
                x.Subtotal,
                x.Discount,
                x.TotalAmount,
                x.PaymentStatus,
                x.CreatedAt,
                Items = x.Items.Select(i => new
                {
                    i.ProductId,
                    Product = i.Product.Name,
                    i.Quantity,
                    i.UnitPrice,
                    i.LineTotal
                })
            })
            .ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create(CreateSaleRequest request)
    {
        if (request.Items.Count == 0)
            return BadRequest(new { message = "At least one sale item is required." });

        if (!await db.Customers.AnyAsync(x => x.Id == request.CustomerId))
            return BadRequest(new { message = "Customer not found." });

        var productIds = request.Items.Select(x => x.ProductId).Distinct().ToArray();
        var products = await db.Products.Where(x => productIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id);

        if (products.Count != productIds.Length)
            return BadRequest(new { message = "One or more products were not found." });

        var requestedByProduct = request.Items
            .GroupBy(x => x.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

        foreach (var (productId, quantity) in requestedByProduct)
        {
            if (quantity <= 0)
                return BadRequest(new { message = "Quantity must be greater than zero." });

            if (products[productId].CurrentStock < quantity)
                return BadRequest(new
                {
                    message = $"Insufficient stock for {products[productId].Name}. Available: {products[productId].CurrentStock}"
                });
        }

        await using var tx = await db.Database.BeginTransactionAsync();

        var sale = new Sale
        {
            InvoiceNo = $"INV-{DateTime.UtcNow:yyyyMMddHHmmss}",
            CustomerId = request.CustomerId,
            Discount = Math.Max(0, request.Discount),
            PaymentStatus = "Paid"
        };

        foreach (var line in request.Items)
        {
            var product = products[line.ProductId];
            var unitPrice = line.UnitPrice > 0 ? line.UnitPrice : product.SalePrice;

            var item = new SaleItem
            {
                ProductId = product.Id,
                Quantity = line.Quantity,
                UnitPrice = unitPrice,
                UnitCost = product.PurchasePrice,
                LineTotal = line.Quantity * unitPrice
            };

            sale.Items.Add(item);
            sale.Subtotal += item.LineTotal;

            product.CurrentStock -= line.Quantity;
            product.UpdatedAt = DateTime.UtcNow;

            db.StockMovements.Add(new StockMovement
            {
                ProductId = product.Id,
                QuantityChange = -line.Quantity,
                Type = "Sale",
                ReferenceType = "Sale",
                ReferenceId = sale.Id,
                Note = sale.InvoiceNo
            });
        }

        sale.TotalAmount = Math.Max(0, sale.Subtotal - sale.Discount);

        db.Sales.Add(sale);
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return Ok(new { sale.Id, sale.InvoiceNo, sale.Subtotal, sale.Discount, sale.TotalAmount });
    }
}
