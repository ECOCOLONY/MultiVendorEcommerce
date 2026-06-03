using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class Order : BaseAuditableEntity
{
    public string CustomerUserId { get; set; } = string.Empty;

    /// <summary>Human-readable order number (e.g. "ORD-20260503-0001"). Indexed, unique.</summary>
    public string OrderNumber { get; set; } = string.Empty;

    public Guid? ShippingAddressId { get; set; }
    public Address? ShippingAddress { get; set; }

    public Guid? BillingAddressId { get; set; }
    public Address? BillingAddress { get; set; }

    // Address snapshot — copied at checkout so later edits/deletes to Address don't mutate the order.
    public string? ShippingFullName { get; set; }
    public string? ShippingPhone { get; set; }
    public string? ShippingLine1 { get; set; }
    public string? ShippingLine2 { get; set; }
    public string? ShippingCity { get; set; }
    public string? ShippingState { get; set; }
    public string? ShippingPostalCode { get; set; }
    public string? ShippingCountry { get; set; }

    public OrderStatus Status { get; set; }

    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }

    public Guid? CouponId { get; set; }
    public Coupon? Coupon { get; set; }

    // Lifecycle timestamps for reporting and timeline UI.
    public DateTime? PlacedAtUtc { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }

    // Customer-initiated post-delivery refund request (RMA).
    public DateTime? RefundRequestedAtUtc { get; set; }
    public string? RefundReason { get; set; }
    public DateTime? RefundedAtUtc { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
}
