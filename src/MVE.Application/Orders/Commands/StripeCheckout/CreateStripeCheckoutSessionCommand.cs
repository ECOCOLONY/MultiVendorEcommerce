using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Orders.SharedCheckout;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Orders.Commands.StripeCheckout;

public sealed record CreateStripeCheckoutSessionCommand(
    Guid ShippingAddressId,
    string SuccessUrl,
    string CancelUrl,
    string? CouponCode = null
) : IRequest<Result<string>>;

public sealed class CreateStripeCheckoutSessionCommandValidator : FluentValidation.AbstractValidator<CreateStripeCheckoutSessionCommand>
{
    public CreateStripeCheckoutSessionCommandValidator()
    {
        RuleFor(x => x.ShippingAddressId).NotEmpty();
        RuleFor(x => x.SuccessUrl).NotEmpty();
        RuleFor(x => x.CancelUrl).NotEmpty();
    }
}

public sealed class CreateStripeCheckoutSessionCommandHandler
    : IRequestHandler<CreateStripeCheckoutSessionCommand, Result<string>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    private readonly IDateTimeService _clock;
    private readonly IStripePaymentService _stripe;
    private readonly IStripeConfigurationProvider _stripeConfig;

    public CreateStripeCheckoutSessionCommandHandler(
        IApplicationDbContext db,
        ICurrentUserService user,
        IDateTimeService clock,
        IStripePaymentService stripe,
        IStripeConfigurationProvider stripeConfig)
    {
        _db = db;
        _user = user;
        _clock = clock;
        _stripe = stripe;
        _stripeConfig = stripeConfig;
    }

    public async Task<Result<string>> Handle(CreateStripeCheckoutSessionCommand req, CancellationToken ct)
    {
        if (!await _stripe.IsConfiguredAsync(ct))
            return Result<string>.Failure("Stripe is not configured. Set keys in Admin → Stripe payments or appsettings.");

        var userId = _user.UserId ?? throw new ForbiddenAccessException();
        var now = _clock.UtcNow;

        var evalResult = await CheckoutOrderComposer.EvaluateAsync(_db, userId, req.ShippingAddressId, req.CouponCode, now, ct);
        if (!evalResult.Succeeded)
            return Result<string>.Failure(evalResult.Errors);

        var ev = evalResult.Value!;
        var total = ev.Subtotal - ev.DiscountAmount;
        var totalCents = (long)Math.Round(total * 100m, MidpointRounding.AwayFromZero);
        if (totalCents <= 0)
            return Result<string>.Failure("Order total must be greater than zero.");

        var snap = await _stripeConfig.GetAsync(ct);
        var stripeCurrency = string.IsNullOrWhiteSpace(snap.Currency) ? "usd" : snap.Currency.Trim().ToLowerInvariant();

        var itemCount = ev.Cart.Items.Sum(i => i.Quantity);
        var description = string.Join("; ", ev.Cart.Items.Take(5).Select(i => $"{i.ProductVariant.Product.Name} x{i.Quantity}"));
        if (ev.Cart.Items.Count > 5) description += "; …";

        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["userId"] = userId,
            ["shippingAddressId"] = req.ShippingAddressId.ToString(),
            ["couponCode"] = req.CouponCode?.Trim() ?? "",
            ["totalCents"] = totalCents.ToString()
        };

        var url = await _stripe.CreateCheckoutSessionAsync(
            req.SuccessUrl,
            req.CancelUrl,
            metadata,
            totalCents,
            stripeCurrency,
            $"Order — {itemCount} item(s)",
            description,
            ct);

        return string.IsNullOrEmpty(url)
            ? Result<string>.Failure("Could not start Stripe Checkout.")
            : Result<string>.Success(url);
    }
}
