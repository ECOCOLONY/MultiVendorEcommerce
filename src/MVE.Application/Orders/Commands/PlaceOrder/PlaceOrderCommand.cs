using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Orders.SharedCheckout;
using Domain.Events;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Orders.Commands.PlaceOrder;

public sealed record PlaceOrderCommand(
    Guid ShippingAddressId,
    Guid? BillingAddressId,
    PaymentMethod PaymentMethod,
    string? CouponCode = null
) : IRequest<Result<Guid>>;

public sealed class PlaceOrderCommandValidator : AbstractValidator<PlaceOrderCommand>
{
    public PlaceOrderCommandValidator()
    {
        RuleFor(x => x.ShippingAddressId).NotEmpty();
    }
}

public sealed class PlaceOrderCommandHandler : IRequestHandler<PlaceOrderCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    private readonly IDateTimeService _clock;
    private readonly IEmailService _email;
    private readonly IIdentityService _identity;

    public PlaceOrderCommandHandler(IApplicationDbContext db, ICurrentUserService user, IDateTimeService clock,
        IEmailService email, IIdentityService identity)
    {
        _db = db; _user = user; _clock = clock; _email = email; _identity = identity;
    }

    public async Task<Result<Guid>> Handle(PlaceOrderCommand req, CancellationToken ct)
    {
        if (req.PaymentMethod == PaymentMethod.Stripe)
            return Result<Guid>.Failure("Card payments use Stripe Checkout — choose Card (Stripe) and confirm payment on the next step.");

        var userId = _user.UserId ?? throw new ForbiddenAccessException();
        var now = _clock.UtcNow;

        var evalResult = await CheckoutOrderComposer.EvaluateAsync(_db, userId, req.ShippingAddressId, req.CouponCode, now, ct);
        if (!evalResult.Succeeded)
            return Result<Guid>.Failure(evalResult.Errors);

        var order = CheckoutOrderComposer.BuildAndAttachOrder(
            evalResult.Value!,
            userId,
            req.BillingAddressId,
            stripePaymentIntentId: null,
            paymentMethod: req.PaymentMethod);

        var cart = evalResult.Value!.Cart;
        _db.CartItems.RemoveRange(cart.Items);
        _db.Orders.Add(order);
        order.AddDomainEvent(new OrderPlacedEvent(order.Id, order.OrderNumber, userId, order.Total));

        await _db.SaveChangesAsync(ct);

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
