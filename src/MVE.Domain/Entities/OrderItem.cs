using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class OrderItem : BaseAuditableEntity
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public Guid ProductVariantId { get; set; }
    public ProductVariant ProductVariant { get; set; } = null!;

    /// <summary>Denormalized for filtering vendor dashboards and commission audit.</summary>
    public Guid VendorStoreId { get; set; }
    public VendorStore VendorStore { get; set; } = null!;

    public string ProductName { get; set; } = string.Empty;
    public string? VariantName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public decimal LineTotal { get; set; }

    /// <summary>Percent snapshot at checkout time (admin/vendor policy).</summary>
    public decimal CommissionPercent { get; set; }

    public decimal CommissionAmount { get; set; }
    public decimal VendorNetAmount { get; set; }

    public VendorOrderItemStatus VendorFulfillmentStatus { get; set; } = VendorOrderItemStatus.PendingFulfillment;
}
