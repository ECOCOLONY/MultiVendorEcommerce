using Domain.Common;

namespace Domain.Events;

public sealed class OrderPlacedEvent : BaseDomainEvent
{
    public Guid OrderId { get; }
    public string OrderNumber { get; }
    public string CustomerUserId { get; }
    public decimal Total { get; }

    public OrderPlacedEvent(Guid orderId, string orderNumber, string customerUserId, decimal total)
    {
        OrderId = orderId;
        OrderNumber = orderNumber;
        CustomerUserId = customerUserId;
        Total = total;
    }
}
