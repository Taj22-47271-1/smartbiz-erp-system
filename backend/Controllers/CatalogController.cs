using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBizERP.Api.Data;
using SmartBizERP.Api.Domain;

namespace SmartBizERP.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/catalog")]
public class CatalogController(AppDbContext db) : ControllerBase
{
    public record CategoryRequest(string Name);
    public record ProductRequest(
        string Name,
        string Sku,
        decimal PurchasePrice,
        decimal SalePrice,
        int ReorderLevel,
        Guid CategoryId);

    [Authorize(Policy = "products.read")]
    [HttpGet("categories")]
    public async Task<IActionResult> Categories() =>
        Ok(await db.Categories
            .OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name })
            .ToListAsync());

    [Authorize(Policy = "permission:products.manage")]
    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory(CategoryRequest request)
    {
        var category = new Category { Name = request.Name.Trim() };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return Ok(category);
    }

    [Authorize(Policy = "products.read")]
    [HttpGet("products")]
    public async Task<IActionResult> Products() =>
        Ok(await db.Products
            .Include(x => x.Category)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Sku,
                x.PurchasePrice,
                x.SalePrice,
                x.CurrentStock,
                x.ReorderLevel,
                Category = x.Category.Name,
                x.CategoryId
            })
            .ToListAsync());

    [Authorize(Policy = "permission:products.manage")]
    [HttpPost("products")]
    public async Task<IActionResult> CreateProduct(ProductRequest request)
    {
        if (!await db.Categories.AnyAsync(x => x.Id == request.CategoryId))
            return BadRequest(new { message = "Category not found." });

        if (await db.Products.AnyAsync(x => x.Sku == request.Sku))
            return Conflict(new { message = "SKU already exists." });

        var product = new Product
        {
            Name = request.Name.Trim(),
            Sku = request.Sku.Trim().ToUpperInvariant(),
            PurchasePrice = request.PurchasePrice,
            SalePrice = request.SalePrice,
            ReorderLevel = request.ReorderLevel,
            CategoryId = request.CategoryId,
            CurrentStock = 0
        };

        db.Products.Add(product);
        await db.SaveChangesAsync();
        return Ok(product);
    }

    [Authorize(Policy = "permission:products.manage")]
    [HttpPut("products/{id:guid}")]
    public async Task<IActionResult> UpdateProduct(Guid id, ProductRequest request)
    {
        var product = await db.Products.FindAsync(id);
        if (product is null) return NotFound();

        if (await db.Products.AnyAsync(x => x.Sku == request.Sku && x.Id != id))
            return Conflict(new { message = "SKU already exists." });

        product.Name = request.Name.Trim();
        product.Sku = request.Sku.Trim().ToUpperInvariant();
        product.PurchasePrice = request.PurchasePrice;
        product.SalePrice = request.SalePrice;
        product.ReorderLevel = request.ReorderLevel;
        product.CategoryId = request.CategoryId;
        product.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(product);
    }

    [Authorize(Policy = "permission:products.manage")]
    [HttpDelete("products/{id:guid}")]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        var product = await db.Products.FindAsync(id);
        if (product is null) return NotFound();

        var hasTransactions =
            await db.PurchaseItems.AnyAsync(x => x.ProductId == id) ||
            await db.SaleItems.AnyAsync(x => x.ProductId == id);

        if (hasTransactions)
            return BadRequest(new { message = "Product has transactions and cannot be deleted." });

        db.Products.Remove(product);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
