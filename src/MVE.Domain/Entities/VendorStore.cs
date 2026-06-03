using Domain.Common;

namespace Domain.Entities;

public class VendorStore : BaseAuditableEntity
{
    public Guid VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public string? LogoUrl { get; set; }
    public string? BannerUrl { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<Coupon> Coupons { get; set; } = new List<Coupon>();
}
