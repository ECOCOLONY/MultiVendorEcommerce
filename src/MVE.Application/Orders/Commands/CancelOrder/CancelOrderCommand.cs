using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Orders.Commands.CancelOrder;

public sealed record CancelOrderCommand(Guid OrderId) : IRequest<Result>;

public sealed class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    private readonly IDateTimeService _clock;
    private readonly IStripePaymentService _stripe;

    public CancelOrderCommandHandler(IApplicationDbContext db, ICurrentUserService user, IDateTimeService clock,
        IStripePaymentService stripe)
    {
        _db = db; _user = user; _clock = clock; _stripe = stripe;
    }

    public async Task<Result> Handle(CancelOrderCommand req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();

        var order = await _db.Orders
            .Include(o => o.Items).ThenInclude(i => i.ProductVariant)
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == req.OrderId, ct);

        if (order is null) return Result.Failure("Order not found.");
        if (order.CustomerUserId != userId) throw new ForbiddenAccessException();

        // Only allow cancel while every item is still pending or processing.
        if (order.Items.Any(i => (int)i.VendorFulfillmentStatus >= (int)VendorOrderItemStatus.ReadyToShip))
            return Result.Failure("Cannot cancel — at least one vendor has already shipped or readied your items.");

        if (order.Status == OrderStatus.Cancelled || order.Status == OrderStatus.Delivered || order.Status == OrderStatus.Refunded)
            return Result.Failure($"Order is already {order.Status}.");

        var captured = order.Payments.FirstOrDefault(p => p.Status == PaymentStatus.Captured);
        if (captured is not null)
        {
            if (IsStripePayment(captured))
            {
                if (!await _stripe.IsConfiguredAsync(ct))
                    return Result.Failure("Cannot cancel — Stripe is not configured, so the card payment cannot be refunded automatically.");
                var (ok, _, err) = await _stripe.RefundPaymentIntentAsync(captured.ExternalPaymentId!, ct);
                if (!ok)
                    return Result.Failure($"Stripe refund failed: {err}");
            }

            captured.Status = PaymentStatus.Refunded;
        }

        // Restore stock + cancel items.
        foreach (var i in order.Items)
        {
            i.VendorFulfillmentStatus = VendorOrderItemStatus.Cancelled;
            i.ProductVariant.StockQuantity += i.Quantity;
        }

        order.Status = OrderStatus.Cancelled;
        order.CancelledAtUtc = _clock.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static bool IsStripePayment(Payment p) =>
        string.Equals(p.Provider, "Stripe", StringComparison.OrdinalIgnoreCase)
        || (!string.IsNullOrEmpty(p.ExternalPaymentId) && p.ExternalPaymentId.StartsWith("pi_", StringComparison.Ordinal));
}
