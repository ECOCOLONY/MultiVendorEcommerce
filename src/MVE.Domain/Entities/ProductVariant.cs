using Domain.Common;

namespace Domain.Entities;

public class ProductVariant : BaseAuditableEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public string Sku { get; set; } = string.Empty;
    public string? Name { get; set; }

    // Variant-defining attributes. Kept as flat optional fields for academic scope;
    // can be replaced with an Attribute / AttributeValue join model later.
    public string? Color { get; set; }
    public string? Size { get; set; }

    public decimal Price { get; set; }
    public int StockQuantity { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
