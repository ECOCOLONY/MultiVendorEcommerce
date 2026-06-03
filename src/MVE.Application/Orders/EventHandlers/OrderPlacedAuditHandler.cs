using Application.Common.Interfaces;
using Domain.Enums;
using Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Orders.EventHandlers;

/// <summary>
/// Demonstrates the domain-event pipeline:
/// Order.AddDomainEvent(new OrderPlacedEvent(...)) is dispatched by
/// DispatchDomainEventsInterceptor after SaveChangesAsync, then this handler
/// writes an AuditLog entry.
/// </summary>
public sealed class OrderPlacedAuditHandler : INotificationHandler<OrderPlacedEvent>
{
    private readonly IAuditLogger _audit;
    private readonly ILogger<OrderPlacedAuditHandler> _logger;

    public OrderPlacedAuditHandler(IAuditLogger audit, ILogger<OrderPlacedAuditHandler> logger)
    {
        _audit = audit;
        _logger = logger;
    }

    public async Task Handle(OrderPlacedEvent notification, CancellationToken ct)
    {
        _logger.LogInformation("OrderPlaced domain event: {OrderNumber} — {Total:0.00}", notification.OrderNumber, notification.Total);

        await _audit.LogAsync(
            AuditAction.Create,
            entityName: "Order",
            entityId: notification.OrderId.ToString(),
            newValuesJson: $"{{\"orderNumber\":\"{notification.OrderNumber}\",\"total\":{notification.Total},\"customer\":\"{notification.CustomerUserId}\"}}",
            ct: ct);
    }
}
