using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Orders.SharedCheckout;
using Domain.Events;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Orders.Commands.StripeCheckout;

public sealed record CompleteStripeCheckoutCommand(string SessionId) : IRequest<Result<Guid>>;

public sealed class CompleteStripeCheckoutCommandHandler : IRequestHandler<CompleteStripeCheckoutCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    private readonly IDateTimeService _clock;
    private readonly IEmailService _email;
    private readonly IIdentityService _identity;
    private readonly IStripePaymentService _stripe;

    public CompleteStripeCheckoutCommandHandler(
        IApplicationDbContext db,
        ICurrentUserService user,
        IDateTimeService clock,
        IEmailService email,
        IIdentityService identity,
        IStripePaymentService stripe)
    {
        _db = db;
        _user = user;
        _clock = clock;
        _email = email;
        _identity = identity;
        _stripe = stripe;
    }

    public async Task<Result<Guid>> Handle(CompleteStripeCheckoutCommand req, CancellationToken ct)
    {
        if (!await _stripe.IsConfiguredAsync(ct))
            return Result<Guid>.Failure("Stripe is not configured.");

        var userId = _user.UserId ?? throw new ForbiddenAccessException();
        var paid = await _stripe.TryGetPaidSessionAsync(req.SessionId, ct);
        if (paid is null)
            return Result<Guid>.Failure("Payment was not completed or session is invalid.");

        var existing = await _db.Payments.AsNoTracking()
            .Where(p => p.ExternalPaymentId == paid.PaymentIntentId)
            .Select(p => p.OrderId)
            .FirstOrDefaultAsync(ct);
        if (existing != Guid.Empty)
            return Result<Guid>.Success(existing);

        if (!paid.Metadata.TryGetValue("userId", out var metaUser) || metaUser != userId)
            return Result<Guid>.Failure("This checkout session does not belong to your account.");

        if (!paid.Metadata.TryGetValue("shippingAddressId", out var addrStr) || !Guid.TryParse(addrStr, out var shippingAddressId))
            return Result<Guid>.Failure("Invalid checkout session (address).");

        paid.Metadata.TryGetValue("couponCode", out var couponCode);
        couponCode = string.IsNullOrWhiteSpace(couponCode) ? null : couponCode;

        if (!paid.Metadata.TryGetValue("totalCents", out var expectedCentsStr) || !long.TryParse(expectedCentsStr, out var expectedCents))
            return Result<Guid>.Failure("Invalid checkout session (amount).");

        if (paid.AmountTotalCents != expectedCents)
            return Result<Guid>.Failure("Paid amount does not match checkout. You have not been charged incorrectly — please contact support if this persists.");

        var now = _clock.UtcNow;
        var evalResult = await CheckoutOrderComposer.EvaluateAsync(_db, userId, shippingAddressId, couponCode, now, ct);
        if (!evalResult.Succeeded)
        {
            var (ok, _, err) = await _stripe.RefundPaymentIntentAsync(paid.PaymentIntentId, ct);
            var msg = string.Join(" ", evalResult.Errors);
            if (!ok)
                msg += $" Automatic refund failed: {err}. Please contact support with Payment ID {paid.PaymentIntentId}.";
            return Result<Guid>.Failure(msg);
        }

        var ev = evalResult.Value!;
        var total = ev.Subtotal - ev.DiscountAmount;
        var cents = (long)Math.Round(total * 100m, MidpointRounding.AwayFromZero);
        if (cents != expectedCents)
        {
            await _stripe.RefundPaymentIntentAsync(paid.PaymentIntentId, ct);
            return Result<Guid>.Failure("Order total changed since checkout was started. Your payment has been refunded.");
        }

        var order = CheckoutOrderComposer.BuildAndAttachOrder(
            ev,
            userId,
            billingAddressId: null,
            stripePaymentIntentId: paid.PaymentIntentId,
            paymentMethod: PaymentMethod.Stripe);

        var cart = ev.Cart;
        _db.CartItems.RemoveRange(cart.Items);
        _db.Orders.Add(order);
        order.AddDomainEvent(new OrderPlacedEvent(order.Id, order.OrderNumber, userId, order.Total));

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch
        {
            await _stripe.RefundPaymentIntentAsync(paid.PaymentIntentId, ct);
            throw;
        }

        var customer = await _identity.GetUserAsync(userId);
        if (customer is not null && !string.IsNullOrEmpty(customer.Email))
        {
            var body = $"<h3>Thanks for your order, {customer.FullName}!</h3>" +
                       $"<p>Order <strong>{order.OrderNumber}</strong> has been placed.</p>" +
                       $"<p>Total: <strong>{order.Total:0.00}</strong></p>";
            await _email.SendAsync(customer.Email, $"Order confirmation — {order.OrderNumber}", body, ct);
        }

        return Result<Guid>.Success(order.Id);
    }
}
