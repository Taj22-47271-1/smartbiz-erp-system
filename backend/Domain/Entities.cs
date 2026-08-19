namespace SmartBizERP.Api.Domain;

public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class Role : BaseEntity
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public class Permission : BaseEntity
{
    public string Key { get; set; } = "";
    public string Description { get; set; } = "";
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public class RolePermission
{
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
}

public class User : BaseEntity
{
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
}

public class Category : BaseEntity
{
    public string Name { get; set; } = "";
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class Product : BaseEntity
{
    public string Name { get; set; } = "";
    public string Sku { get; set; } = "";
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public int CurrentStock { get; set; }
    public int ReorderLevel { get; set; } = 5;
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}

public class Customer : BaseEntity
{
    public string Name { get; set; } = "";
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
}

public class Supplier : BaseEntity
{
    public string Name { get; set; } = "";
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
}

public class Purchase : BaseEntity
{
    public string PurchaseNo { get; set; } = "";
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Received";
    public ICollection<PurchaseItem> Items { get; set; } = new List<PurchaseItem>();
}

public class PurchaseItem : BaseEntity
{
    public Guid PurchaseId { get; set; }
    public Purchase Purchase { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineTotal { get; set; }
}

public class Sale : BaseEntity
{
    public string InvoiceNo { get; set; } = "";
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentStatus { get; set; } = "Paid";
    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
}

public class SaleItem : BaseEntity
{
    public Guid SaleId { get; set; }
    public Sale Sale { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineTotal { get; set; }
}

public class StockMovement : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int QuantityChange { get; set; }
    public string Type { get; set; } = "";
    public string ReferenceType { get; set; } = "";
    public Guid? ReferenceId { get; set; }
    public string? Note { get; set; }
}

public class Expense : BaseEntity
{
    public string Category { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; } = DateTime.UtcNow;
}

public class AuditLog : BaseEntity
{
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string Method { get; set; } = "";
    public string Path { get; set; } = "";
    public int StatusCode { get; set; }
    public string? IpAddress { get; set; }
}

public class AttendanceSetting : BaseEntity
{
    public TimeOnly WorkStartTime { get; set; } = new(9, 0);
    public TimeOnly LateAfterTime { get; set; } = new(9, 15);
    public TimeOnly WorkEndTime { get; set; } = new(17, 0);
    public TimeOnly AutoCheckoutTime { get; set; } = new(18, 0);
    public string TimeZoneId { get; set; } = "Asia/Dhaka";
    public string WorkingDays { get; set; } = "Sunday,Monday,Tuesday,Wednesday,Thursday";
    public bool IsAutoCheckoutEnabled { get; set; } = true;
}

public class AttendanceRecord : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public DateOnly AttendanceDate { get; set; }
    public DateTime CheckInAt { get; set; }
    public DateTime? CheckOutAt { get; set; }
    public string Status { get; set; } = "Present";
    public string? CheckOutType { get; set; }
    public string? Note { get; set; }
}
