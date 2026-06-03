namespace Domain.Enums;

public enum VendorOrderItemStatus
{
    PendingFulfillment = 0,
    Processing = 1,
    ReadyToShip = 2,
    Shipped = 3,
    Delivered = 4,
    Cancelled = 5
}
