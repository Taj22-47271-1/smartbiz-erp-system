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
    public DbSet<AttendanceSetting> AttendanceSettings => Set<AttendanceSetting>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<Permission>().HasIndex(x => x.Key).IsUnique();
        modelBuilder.Entity<User>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<Product>().HasIndex(x => x.Sku).IsUnique();
        modelBuilder.Entity<Purchase>().HasIndex(x => x.PurchaseNo).IsUnique();
        modelBuilder.Entity<Sale>().HasIndex(x => x.InvoiceNo).IsUnique();
        modelBuilder.Entity<AttendanceRecord>()
            .HasIndex(x => new { x.UserId, x.AttendanceDate })
            .IsUnique();

        modelBuilder.Entity<RolePermission>().HasKey(x => new { x.RoleId, x.PermissionId });

        modelBuilder.Entity<AttendanceRecord>()
            .HasOne(x => x.User)
            .WithMany(x => x.AttendanceRecords)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

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
    private static readonly (string Key, string Description)[] PermissionDefinitions =
    {
        ("dashboard.view", "View dashboard"),
        ("products.manage", "Manage products"),
        ("customers.manage", "Manage customers"),
        ("suppliers.manage", "Manage suppliers"),
        ("purchases.manage", "Manage purchases"),
        ("sales.manage", "Manage sales"),
        ("expenses.manage", "Manage expenses"),
        ("users.manage", "Manage users and roles"),
        ("reports.view", "View reports"),
        ("attendance.checkin", "Check in and check out own attendance"),
        ("attendance.view", "View employee attendance and summaries"),
        ("attendance.manage", "Manage attendance settings")
    };

    public static async Task InitializeAsync(AppDbContext db)
    {
        await EnsurePermissionsAsync(db);
        await EnsureRolesAsync(db);
        await EnsureAttendanceSettingAsync(db);
        await EnsureDemoAdminAsync(db);
        await EnsureDemoBusinessDataAsync(db);
    }

    private static async Task EnsurePermissionsAsync(AppDbContext db)
    {
        var existing = await db.Permissions.Select(x => x.Key).ToListAsync();
        var missing = PermissionDefinitions
            .Where(x => !existing.Contains(x.Key))
            .Select(x => new Permission { Key = x.Key, Description = x.Description })
            .ToList();

        if (missing.Count == 0) return;
        db.Permissions.AddRange(missing);
        await db.SaveChangesAsync();
    }

    private static async Task EnsureRolesAsync(AppDbContext db)
    {
        var adminRole = await db.Roles.FirstOrDefaultAsync(x => x.Name == "Administrator");
        if (adminRole is null)
        {
            adminRole = new Role { Name = "Administrator", Description = "Full system access" };
            db.Roles.Add(adminRole);
            await db.SaveChangesAsync();
        }

        var salesRole = await db.Roles.FirstOrDefaultAsync(x => x.Name == "Sales Manager");
        if (salesRole is null)
        {
            salesRole = new Role { Name = "Sales Manager", Description = "Sales and customer operations" };
            db.Roles.Add(salesRole);
            await db.SaveChangesAsync();
        }

        var permissions = await db.Permissions.ToListAsync();
        var existingAdminPermissionIds = await db.RolePermissions
            .Where(x => x.RoleId == adminRole.Id)
            .Select(x => x.PermissionId)
            .ToListAsync();

        var adminMissing = permissions
            .Where(p => !existingAdminPermissionIds.Contains(p.Id))
            .Select(p => new RolePermission { RoleId = adminRole.Id, PermissionId = p.Id })
            .ToList();
        if (adminMissing.Count > 0) db.RolePermissions.AddRange(adminMissing);

        var salesPermissionKeys = new[]
        {
            "dashboard.view", "customers.manage", "sales.manage", "reports.view", "attendance.checkin"
        };
        var salesPermissionIds = permissions.Where(p => salesPermissionKeys.Contains(p.Key)).Select(p => p.Id).ToList();
        var existingSalesPermissionIds = await db.RolePermissions
            .Where(x => x.RoleId == salesRole.Id)
            .Select(x => x.PermissionId)
            .ToListAsync();

        var salesMissing = salesPermissionIds
            .Where(id => !existingSalesPermissionIds.Contains(id))
            .Select(id => new RolePermission { RoleId = salesRole.Id, PermissionId = id })
            .ToList();
        if (salesMissing.Count > 0) db.RolePermissions.AddRange(salesMissing);

        await db.SaveChangesAsync();
    }

    private static async Task EnsureAttendanceSettingAsync(AppDbContext db)
    {
        if (await db.AttendanceSettings.AnyAsync()) return;

        db.AttendanceSettings.Add(new AttendanceSetting
        {
            WorkStartTime = new TimeOnly(9, 0),
            LateAfterTime = new TimeOnly(9, 15),
            WorkEndTime = new TimeOnly(17, 0),
            AutoCheckoutTime = new TimeOnly(18, 0),
            TimeZoneId = "Asia/Dhaka",
            WorkingDays = "Sunday,Monday,Tuesday,Wednesday,Thursday",
            IsAutoCheckoutEnabled = true
        });
        await db.SaveChangesAsync();
    }

    private static async Task EnsureDemoAdminAsync(AppDbContext db)
    {
        if (await db.Users.AnyAsync(x => x.Email == "admin@smartbiz.local")) return;

        var adminRole = await db.Roles.FirstAsync(x => x.Name == "Administrator");
        var admin = new User
        {
            FullName = "System Administrator",
            Email = "admin@smartbiz.local",
            RoleId = adminRole.Id
        };

        var hasher = new PasswordHasher<User>();
        admin.PasswordHash = hasher.HashPassword(admin, "Admin123!");
        db.Users.Add(admin);
        await db.SaveChangesAsync();
    }

    private static async Task EnsureDemoBusinessDataAsync(AppDbContext db)
    {
        if (!await db.Categories.AnyAsync())
        {
            db.Categories.AddRange(
                new Category { Name = "Electronics" },
                new Category { Name = "Accessories" }
            );
            await db.SaveChangesAsync();
        }

        if (!await db.Customers.AnyAsync())
        {
            db.Customers.Add(new Customer
            {
                Name = "Demo Retail Customer",
                Phone = "01700000000",
                Email = "customer@example.com",
                Address = "Dhaka, Bangladesh"
            });
        }

        if (!await db.Suppliers.AnyAsync())
        {
            db.Suppliers.Add(new Supplier
            {
                Name = "Dhaka Tech Supply",
                Phone = "01800000000",
                Email = "supplier@example.com",
                Address = "Dhaka, Bangladesh"
            });
        }

        await db.SaveChangesAsync();

        if (!await db.Products.AnyAsync())
        {
            var electronics = await db.Categories.FirstAsync(x => x.Name == "Electronics");
            var accessories = await db.Categories.FirstAsync(x => x.Name == "Accessories");
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
        }

        if (!await db.Expenses.AnyAsync())
        {
            db.Expenses.Add(new Expense
            {
                Category = "Office",
                Description = "Internet and utilities",
                Amount = 5500,
                ExpenseDate = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
    }
}
