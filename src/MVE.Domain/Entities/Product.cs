using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class Product : BaseAuditableEntity
{
    public Guid VendorStoreId { get; set; }
    public VendorStore VendorStore { get; set; } = null!;

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public bool IsPublished { get; set; }
    public bool IsFeatured { get; set; }

    /// <summary>Lifecycle gate controlled only by Admin (vendors submit Pending).</summary>
    public ProductApprovalStatus ApprovalStatus { get; set; } = ProductApprovalStatus.Pending;

    // Denormalized stats — recomputed on review create/delete and on product detail view.
    // Keeping them on the row keeps listing queries cheap (no GROUP BY at read time).
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public int ViewCount { get; set; }

    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
}
