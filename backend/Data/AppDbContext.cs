using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartBizERP.Api.Domain;

namespace SmartBizERP.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<PurchaseItem> PurchaseItems => Set<PurchaseItem>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<Permission>().HasIndex(x => x.Key).IsUnique();
        modelBuilder.Entity<User>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<Product>().HasIndex(x => x.Sku).IsUnique();
        modelBuilder.Entity<Purchase>().HasIndex(x => x.PurchaseNo).IsUnique();
        modelBuilder.Entity<Sale>().HasIndex(x => x.InvoiceNo).IsUnique();

        modelBuilder.Entity<RolePermission>().HasKey(x => new { x.RoleId, x.PermissionId });

        modelBuilder.Entity<Product>().Property(x => x.PurchasePrice).HasPrecision(18, 2);
        modelBuilder.Entity<Product>().Property(x => x.SalePrice).HasPrecision(18, 2);
        modelBuilder.Entity<Purchase>().Property(x => x.TotalAmount).HasPrecision(18, 2);
        modelBuilder.Entity<PurchaseItem>().Property(x => x.UnitCost).HasPrecision(18, 2);
        modelBuilder.Entity<PurchaseItem>().Property(x => x.LineTotal).HasPrecision(18, 2);
        modelBuilder.Entity<Sale>().Property(x => x.Subtotal).HasPrecision(18, 2);
        modelBuilder.Entity<Sale>().Property(x => x.Discount).HasPrecision(18, 2);
        modelBuilder.Entity<Sale>().Property(x => x.TotalAmount).HasPrecision(18, 2);
        modelBuilder.Entity<SaleItem>().Property(x => x.UnitPrice).HasPrecision(18, 2);
        modelBuilder.Entity<SaleItem>().Property(x => x.UnitCost).HasPrecision(18, 2);
        modelBuilder.Entity<SaleItem>().Property(x => x.LineTotal).HasPrecision(18, 2);
        modelBuilder.Entity<Expense>().Property(x => x.Amount).HasPrecision(18, 2);

        base.OnModelCreating(modelBuilder);
    }
}

public static class SeedData
{
    public static async Task InitializeAsync(AppDbContext db)
    {
        if (await db.Roles.AnyAsync()) return;

        var permissions = new[]
        {
            new Permission { Key = "dashboard.view", Description = "View dashboard" },
            new Permission { Key = "products.manage", Description = "Manage products" },
            new Permission { Key = "customers.manage", Description = "Manage customers" },
            new Permission { Key = "suppliers.manage", Description = "Manage suppliers" },
            new Permission { Key = "purchases.manage", Description = "Manage purchases" },
            new Permission { Key = "sales.manage", Description = "Manage sales" },
            new Permission { Key = "expenses.manage", Description = "Manage expenses" },
            new Permission { Key = "users.manage", Description = "Manage users and roles" },
            new Permission { Key = "reports.view", Description = "View reports" }
        };

        var adminRole = new Role
        {
            Name = "Administrator",
            Description = "Full system access"
        };

        var salesRole = new Role
        {
            Name = "Sales Manager",
            Description = "Sales and customer operations"
        };

        db.Permissions.AddRange(permissions);
        db.Roles.AddRange(adminRole, salesRole);
        await db.SaveChangesAsync();

        db.RolePermissions.AddRange(permissions.Select(p => new RolePermission
        {
            RoleId = adminRole.Id,
            PermissionId = p.Id
        }));

        var salesPermissionKeys = new[] { "dashboard.view", "customers.manage", "sales.manage", "reports.view" };
        db.RolePermissions.AddRange(
            permissions
                .Where(p => salesPermissionKeys.Contains(p.Key))
                .Select(p => new RolePermission { RoleId = salesRole.Id, PermissionId = p.Id })
        );

        var admin = new User
        {
            FullName = "System Administrator",
            Email = "admin@smartbiz.local",
            RoleId = adminRole.Id
        };

        var hasher = new PasswordHasher<User>();
        admin.PasswordHash = hasher.HashPassword(admin, "Admin123!");
        db.Users.Add(admin);

        var electronics = new Category { Name = "Electronics" };
        var accessories = new Category { Name = "Accessories" };
        db.Categories.AddRange(electronics, accessories);

        var customer = new Customer
        {
            Name = "Demo Retail Customer",
            Phone = "01700000000",
            Email = "customer@example.com",
            Address = "Dhaka, Bangladesh"
        };

        var supplier = new Supplier
        {
            Name = "Dhaka Tech Supply",
            Phone = "01800000000",
            Email = "supplier@example.com",
            Address = "Dhaka, Bangladesh"
        };

        db.Customers.Add(customer);
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync();

        db.Products.AddRange(
            new Product
            {
                Name = "Wireless Keyboard",
                Sku = "KB-001",
                CategoryId = accessories.Id,
                PurchasePrice = 2200,
                SalePrice = 2990,
                CurrentStock = 18,
                ReorderLevel = 5
            },
            new Product
            {
                Name = "24-inch Monitor",
                Sku = "MON-024",
                CategoryId = electronics.Id,
                PurchasePrice = 14500,
                SalePrice = 17900,
                CurrentStock = 7,
                ReorderLevel = 3
            }
        );

        db.Expenses.Add(new Expense
        {
            Category = "Office",
            Description = "Internet and utilities",
            Amount = 5500,
            ExpenseDate = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }
}
