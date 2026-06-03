using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Coupons.Queries;

public sealed record CouponDto(
    Guid Id,
    string Code,
    CouponDiscountType DiscountType,
    decimal DiscountValue,
    decimal MinimumOrderAmount,
    int? MaxUses,
    int? MaxUsesPerCustomer,
    int UsedCount,
    DateTime? StartsAtUtc,
    DateTime? ExpiresAtUtc,
    bool IsActive,
    Guid? VendorStoreId,
    string? StoreName
);

// ----- ADMIN: platform-wide coupons (VendorStoreId == null) -----
public sealed record GetPlatformCouponsQuery(
    string? Search = null,
    bool? IsActive = null,
    int Page = 1,
    int PageSize = 15
) : IRequest<PaginatedList<CouponDto>>;

public sealed class GetPlatformCouponsQueryHandler : IRequestHandler<GetPlatformCouponsQuery, PaginatedList<CouponDto>>
{
    private readonly IApplicationDbContext _db;
    public GetPlatformCouponsQueryHandler(IApplicationDbContext db) => _db = db;

    public Task<PaginatedList<CouponDto>> Handle(GetPlatformCouponsQuery req, CancellationToken ct)
    {
        var q = _db.Coupons.Where(c => c.VendorStoreId == null);
        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.Trim().ToUpperInvariant();
            q = q.Where(c => c.Code.Contains(s));
        }
        if (req.IsActive.HasValue) q = q.Where(c => c.IsActive == req.IsActive.Value);

        var projection = q
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new CouponDto(c.Id, c.Code, c.DiscountType, c.DiscountValue, c.MinimumOrderAmount,
                c.MaxUses, c.MaxUsesPerCustomer, c.UsedCount, c.StartsAtUtc, c.ExpiresAtUtc,
                c.IsActive, c.VendorStoreId, null));

        return PaginatedList<CouponDto>.CreateAsync(projection, req.Page, req.PageSize, ct);
    }
}

// ----- VENDOR: my coupons -----
public sealed record GetMyCouponsQuery(
    string? Search = null,
    bool? IsActive = null,
    int Page = 1,
    int PageSize = 15
) : IRequest<PaginatedList<CouponDto>>;

public sealed class GetMyCouponsQueryHandler : IRequestHandler<GetMyCouponsQuery, PaginatedList<CouponDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public GetMyCouponsQueryHandler(IApplicationDbContext db, ICurrentUserService user)
    { _db = db; _user = user; }

    public Task<PaginatedList<CouponDto>> Handle(GetMyCouponsQuery req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();
        var q = _db.Coupons.Where(c => c.VendorStore != null && c.VendorStore.Vendor.OwnerUserId == userId);
        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.Trim().ToUpperInvariant();
            q = q.Where(c => c.Code.Contains(s));
        }
        if (req.IsActive.HasValue) q = q.Where(c => c.IsActive == req.IsActive.Value);

        var projection = q
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new CouponDto(c.Id, c.Code, c.DiscountType, c.DiscountValue, c.MinimumOrderAmount,
                c.MaxUses, c.MaxUsesPerCustomer, c.UsedCount, c.StartsAtUtc, c.ExpiresAtUtc,
                c.IsActive, c.VendorStoreId, c.VendorStore!.Name));

        return PaginatedList<CouponDto>.CreateAsync(projection, req.Page, req.PageSize, ct);
    }
}

// ----- BY ID -----
public sealed record GetCouponByIdQuery(Guid Id, bool IsAdmin) : IRequest<CouponDto>;

public sealed class GetCouponByIdQueryHandler : IRequestHandler<GetCouponByIdQuery, CouponDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public GetCouponByIdQueryHandler(IApplicationDbContext db, ICurrentUserService user)
    { _db = db; _user = user; }

    public async Task<CouponDto> Handle(GetCouponByIdQuery req, CancellationToken ct)
    {
        var coupon = await _db.Coupons
            .Include(c => c.VendorStore).ThenInclude(s => s!.Vendor)
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct);

        if (coupon is null) throw new NotFoundException(nameof(Domain.Entities.Coupon), req.Id);

        if (!req.IsAdmin)
        {
            var userId = _user.UserId ?? throw new ForbiddenAccessException();
            if (coupon.VendorStore?.Vendor.OwnerUserId != userId) throw new ForbiddenAccessException();
        }
        else if (coupon.VendorStoreId is not null)
        {
            throw new ForbiddenAccessException("This coupon belongs to a vendor.");
        }

        return new CouponDto(coupon.Id, coupon.Code, coupon.DiscountType, coupon.DiscountValue,
            coupon.MinimumOrderAmount, coupon.MaxUses, coupon.MaxUsesPerCustomer, coupon.UsedCount,
            coupon.StartsAtUtc, coupon.ExpiresAtUtc, coupon.IsActive,
            coupon.VendorStoreId, coupon.VendorStore?.Name);
    }
}
