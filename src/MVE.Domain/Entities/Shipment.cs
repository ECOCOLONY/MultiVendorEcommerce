using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class Shipment : BaseAuditableEntity
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    /// <summary>
    /// Shipments are scoped to a single vendor store, since a multi-vendor order
    /// produces one shipment per vendor (each vendor ships their own items).
    /// </summary>
    public Guid VendorStoreId { get; set; }
    public VendorStore VendorStore { get; set; } = null!;

    /// <summary>Optional delivery/courier user assigned to this shipment.</summary>
    public string? AssignedDeliveryUserId { get; set; }

    public string? Carrier { get; set; }
    public string? TrackingNumber { get; set; }
    public ShipmentStatus Status { get; set; }

    public DateTime? ShippedAtUtc { get; set; }
    public DateTime? EstimatedDeliveryAtUtc { get; set; }
    public DateTime? DeliveredAtUtc { get; set; }
}
