using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Refunds.Commands;

// ----- Approve -----
public sealed record ApproveRefundCommand(Guid OrderId) : IRequest<Result>;

public sealed class ApproveRefundCommandHandler : IRequestHandler<ApproveRefundCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeService _clock;
    private readonly IRealtimeNotifier _rt;
    private readonly IStripePaymentService _stripe;

    public ApproveRefundCommandHandler(
        IApplicationDbContext db,
        IDateTimeService clock,
        IRealtimeNotifier rt,
        IStripePaymentService stripe)
    {
        _db = db; _clock = clock; _rt = rt; _stripe = stripe;
    }

    public async Task<Result> Handle(ApproveRefundCommand req, CancellationToken ct)
    {
        var order = await _db.Orders
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == req.OrderId, ct);
        if (order is null) return Result.Failure("Order not found.");
        if (!order.RefundRequestedAtUtc.HasValue) return Result.Failure("No refund request on file.");

        var captured = order.Payments.FirstOrDefault(p => p.Status == PaymentStatus.Captured);
        if (captured is not null)
        {
            if (IsStripePayment(captured))
            {
                if (!await _stripe.IsConfiguredAsync(ct))
                    return Result.Failure("Refund could not be sent to Stripe — keys are not configured.");
                var (ok, _, err) = await _stripe.RefundPaymentIntentAsync(captured.ExternalPaymentId!, ct);
                if (!ok)
                    return Result.Failure($"Stripe refund failed: {err}");
            }

            captured.Status = PaymentStatus.Refunded;
        }

        order.Status = OrderStatus.Refunded;
        order.RefundedAtUtc = _clock.UtcNow;

        var title = "Refund approved";
        var body  = $"Your refund for order {order.OrderNumber} has been approved.";
        var url   = $"/Orders/Details/{order.Id}";

        _db.Notifications.Add(new Notification
        {
            UserId = order.CustomerUserId,
            Title = title, Body = body, Type = NotificationType.OrderUpdate, ActionUrl = url
        });

        await _db.SaveChangesAsync(ct);
        await _rt.NotifyUserAsync(order.CustomerUserId, title, body, url, ct);
        return Result.Success();
    }

    private static bool IsStripePayment(Payment p) =>
        string.Equals(p.Provider, "Stripe", StringComparison.OrdinalIgnoreCase)
        || (!string.IsNullOrEmpty(p.ExternalPaymentId) && p.ExternalPaymentId.StartsWith("pi_", StringComparison.Ordinal));
}

// ----- Reject -----
public sealed record RejectRefundCommand(Guid OrderId, string? Note = null) : IRequest<Result>;

public sealed class RejectRefundCommandHandler : IRequestHandler<RejectRefundCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly IRealtimeNotifier _rt;
    public RejectRefundCommandHandler(IApplicationDbContext db, IRealtimeNotifier rt) { _db = db; _rt = rt; }

    public async Task<Result> Handle(RejectRefundCommand req, CancellationToken ct)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == req.OrderId, ct);
        if (order is null) return Result.Failure("Order not found.");
        if (!order.RefundRequestedAtUtc.HasValue) return Result.Failure("No refund request on file.");

        order.RefundRequestedAtUtc = null;
        order.RefundReason = null;

        var title = "Refund declined";
        var body  = $"Your refund request for order {order.OrderNumber} was not approved." + (string.IsNullOrEmpty(req.Note) ? "" : $" Note: {req.Note}");
        var url   = $"/Orders/Details/{order.Id}";

        _db.Notifications.Add(new Notification
        {
            UserId = order.CustomerUserId,
            Title = title, Body = body, Type = NotificationType.OrderUpdate, ActionUrl = url
        });

        await _db.SaveChangesAsync(ct);
        await _rt.NotifyUserAsync(order.CustomerUserId, title, body, url, ct);
        return Result.Success();
    }
}
