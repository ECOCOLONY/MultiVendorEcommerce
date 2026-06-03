using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Coupons.Queries.ValidateCoupon;

public sealed record ValidatedCouponDto(Guid Id, string Code, decimal DiscountAmount);

public sealed record ValidateCouponQuery(string Code, decimal CartSubtotal) : IRequest<Result<ValidatedCouponDto>>;

public sealed class ValidateCouponQueryHandler : IRequestHandler<ValidateCouponQuery, Result<ValidatedCouponDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeService _clock;

    public ValidateCouponQueryHandler(IApplicationDbContext db, IDateTimeService clock)
    { _db = db; _clock = clock; }

    public async Task<Result<ValidatedCouponDto>> Handle(ValidateCouponQuery req, CancellationToken ct)
    {
        var code = req.Code.Trim().ToUpperInvariant();
        var c = await _db.Coupons.FirstOrDefaultAsync(x => x.Code == code, ct);
        if (c is null) return Result<ValidatedCouponDto>.Failure("Coupon not found.");

        var now = _clock.UtcNow;
        if (!c.IsActive) return Result<ValidatedCouponDto>.Failure("Coupon is not active.");
        if (c.StartsAtUtc.HasValue  && now < c.StartsAtUtc.Value)  return Result<ValidatedCouponDto>.Failure("Coupon is not yet valid.");
        if (c.ExpiresAtUtc.HasValue && now > c.ExpiresAtUtc.Value) return Result<ValidatedCouponDto>.Failure("Coupon has expired.");
        if (c.MaxUses.HasValue && c.UsedCount >= c.MaxUses.Value)  return Result<ValidatedCouponDto>.Failure("Coupon usage limit reached.");
        if (req.CartSubtotal < c.MinimumOrderAmount)
            return Result<ValidatedCouponDto>.Failure($"Minimum order amount is {c.MinimumOrderAmount:0.00}.");

        var discount = c.DiscountType == CouponDiscountType.Percentage
            ? Math.Round(req.CartSubtotal * c.DiscountValue / 100m, 2)
            : Math.Min(c.DiscountValue, req.CartSubtotal);

        return Result<ValidatedCouponDto>.Success(new ValidatedCouponDto(c.Id, c.Code, discount));
    }
}
