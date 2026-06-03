using Domain.Common;

namespace Domain.Entities;

public class Vendor : BaseAuditableEntity
{
    public string OwnerUserId { get; set; } = string.Empty;

    public string BusinessName { get; set; } = string.Empty;
    public string? TaxNumber { get; set; }
    public bool IsApproved { get; set; }

    /// <summary>Applied when orders are placed (snapshot onto order lines); editable only by Admin.</summary>
    public decimal DefaultCommissionPercent { get; set; } = 10m;

    public ICollection<VendorStore> Stores { get; set; } = new List<VendorStore>();
}
