using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Orders.SharedCheckout;

/// <summary>Shared cart + coupon evaluation and order construction for standard checkout and Stripe return.</summary>
public sealed record CheckoutEvaluation(
    Domain.Entities.Cart Cart,
    Address Address,
    decimal Subtotal,
    decimal DiscountAmount,
    Coupon? Coupon,
    DateTime UtcNow);

public static class CheckoutOrderComposer
{
    public static async Task<Result<CheckoutEvaluation>> EvaluateAsync(
        IApplicationDbContext db,
        string userId,
        Guid shippingAddressId,
        string? couponCode,
        DateTime now,
        CancellationToken ct)
    {
        var address = await db.Addresses.FirstOrDefaultAsync(a => a.Id == shippingAddressId, ct);
        if (address is null || address.UserId != userId)
            return Result<CheckoutEvaluation>.Failure("Shipping address not found.");

        var cart = await db.Carts
            .Include(c => c.Items).ThenInclude(i => i.ProductVariant).ThenInclude(v => v.Product).ThenInclude(p => p.VendorStore).ThenInclude(s => s.Vendor)
            .FirstOrDefaultAsync(c => c.CustomerUserId == userId, ct);

        if (cart is null || !cart.Items.Any())
            return Result<CheckoutEvaluation>.Failure("Your cart is empty.");

        foreach (var i in cart.Items)
        {
            if (!i.ProductVariant.IsActive
                || !i.ProductVariant.Product.IsPublished
                || i.ProductVariant.Product.ApprovalStatus != ProductApprovalStatus.Approved)
                return Result<CheckoutEvaluation>.Failure($"'{i.ProductVariant.Product.Name}' is no longer available.");

            if (i.ProductVariant.StockQuantity < i.Quantity)
                return Result<CheckoutEvaluation>.Failure($"Not enough stock for '{i.ProductVariant.Product.Name}'.");
        }

        decimal subtotal = cart.Items.Sum(ci => ci.UnitPrice * ci.Quantity);
        Coupon? coupon = null;
        decimal discount = 0m;

        if (!string.IsNullOrWhiteSpace(couponCode))
        {
            var code = couponCode.Trim().ToUpperInvariant();
            coupon = await db.Coupons.FirstOrDefaultAsync(c => c.Code == code, ct);
            if (coupon is null) return Result<CheckoutEvaluation>.Failure("Coupon not found.");
            if (!coupon.IsActive) return Result<CheckoutEvaluation>.Failure("Coupon is not active.");
            if (coupon.StartsAtUtc.HasValue && now < coupon.StartsAtUtc.Value) return Result<CheckoutEvaluation>.Failure("Coupon is not yet valid.");
            if (coupon.ExpiresAtUtc.HasValue && now > coupon.ExpiresAtUtc.Value) return Result<CheckoutEvaluation>.Failure("Coupon has expired.");
            if (coupon.MaxUses.HasValue && coupon.UsedCount >= coupon.MaxUses.Value) return Result<CheckoutEvaluation>.Failure("Coupon usage limit reached.");
            if (subtotal < coupon.MinimumOrderAmount) return Result<CheckoutEvaluation>.Failure($"Minimum order amount {coupon.MinimumOrderAmount:0.00} not met.");

            if (coupon.MaxUsesPerCustomer.HasValue)
            {
                var myUses = await db.Orders.CountAsync(o => o.CouponId == coupon.Id && o.CustomerUserId == userId, ct);
                if (myUses >= coupon.MaxUsesPerCustomer.Value)
                    return Result<CheckoutEvaluation>.Failure("You have reached your usage limit for this coupon.");
            }

            discount = coupon.DiscountType == CouponDiscountType.Percentage
                ? Math.Round(subtotal * coupon.DiscountValue / 100m, 2)
                : Math.Min(coupon.DiscountValue, subtotal);
        }

        return Result<CheckoutEvaluation>.Success(new CheckoutEvaluation(cart, address, subtotal, discount, coupon, now));
    }

    public static Order BuildAndAttachOrder(
        CheckoutEvaluation ev,
        string userId,
        Guid? billingAddressId,
        string? stripePaymentIntentId,
        PaymentMethod paymentMethod)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerUserId = userId,
            OrderNumber = GenerateOrderNumber(ev.UtcNow),
            ShippingAddressId = ev.Address.Id,
            BillingAddressId = billingAddressId ?? ev.Address.Id,
            ShippingFullName = ev.Address.Label,
            ShippingPhone = ev.Address.Phone,
            ShippingLine1 = ev.Address.Line1,
            ShippingLine2 = ev.Address.Line2,
            ShippingCity = ev.Address.City,
            ShippingState = ev.Address.State,
            ShippingPostalCode = ev.Address.PostalCode,
            ShippingCountry = ev.Address.Country,
            Status = OrderStatus.Paid,
            PlacedAtUtc = ev.UtcNow,
            PaidAtUtc = ev.UtcNow,
            Subtotal = ev.Subtotal,
            ShippingAmount = 0m,
            TaxAmount = 0m,
            DiscountAmount = ev.DiscountAmount,
            Total = ev.Subtotal - ev.DiscountAmount
        };

        if (ev.Coupon is not null)
        {
            order.CouponId = ev.Coupon.Id;
            ev.Coupon.UsedCount += 1;
        }

        foreach (var ci in ev.Cart.Items)
        {
            var v = ci.ProductVariant;
            var lineTotal = ci.UnitPrice * ci.Quantity;
            var commissionPct = v.Product.VendorStore.Vendor.DefaultCommissionPercent;
            var commissionAmt = Math.Round(lineTotal * commissionPct / 100m, 2);

            order.Items.Add(new OrderItem
            {
                ProductVariantId = v.Id,
                VendorStoreId = v.Product.VendorStoreId,
                ProductName = v.Product.Name,
                VariantName = v.Name ?? string.Join(" / ", new[] { v.Color, v.Size }.Where(s => !string.IsNullOrEmpty(s))),
                Quantity = ci.Quantity,
                UnitPrice = ci.UnitPrice,
                LineTotal = lineTotal,
                CommissionPercent = commissionPct,
                CommissionAmount = commissionAmt,
                VendorNetAmount = lineTotal - commissionAmt,
                VendorFulfillmentStatus = VendorOrderItemStatus.PendingFulfillment
            });

            v.StockQuantity -= ci.Quantity;
        }

        var isStripe = paymentMethod == PaymentMethod.Stripe;
        order.Payments.Add(new Payment
        {
            Amount = order.Total,
            Status = PaymentStatus.Captured,
            Provider = isStripe ? "Stripe" : paymentMethod.ToString(),
            ExternalPaymentId = isStripe ? stripePaymentIntentId : null
        });

        return order;
    }

    private static string GenerateOrderNumber(DateTime utcNow) =>
        $"ORD-{utcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
}
