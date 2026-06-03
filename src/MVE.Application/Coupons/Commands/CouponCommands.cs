using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Coupons.Commands;

// ----- CREATE -----
public sealed record CreateCouponCommand(
    string Code,
    CouponDiscountType DiscountType,
    decimal DiscountValue,
    decimal MinimumOrderAmount,
    int? MaxUses,
    int? MaxUsesPerCustomer,
    DateTime? StartsAtUtc,
    DateTime? ExpiresAtUtc,
    bool IsActive,
    Guid? VendorStoreId  // null → admin/platform; set → vendor's own
) : IRequest<Result<Guid>>;

public sealed class CreateCouponCommandValidator : AbstractValidator<CreateCouponCommand>
{
    public CreateCouponCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().Length(2, 40);
        RuleFor(x => x.DiscountValue).GreaterThan(0);
        RuleFor(x => x.DiscountValue).LessThanOrEqualTo(100m)
            .When(x => x.DiscountType == CouponDiscountType.Percentage)
            .WithMessage("Percentage discount must be ≤ 100.");
        RuleFor(x => x.MinimumOrderAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxUses).GreaterThan(0).When(x => x.MaxUses.HasValue);
        RuleFor(x => x.MaxUsesPerCustomer).GreaterThan(0).When(x => x.MaxUsesPerCustomer.HasValue);
        RuleFor(x => x).Must(x => !x.ExpiresAtUtc.HasValue || !x.StartsAtUtc.HasValue || x.ExpiresAtUtc > x.StartsAtUtc)
            .WithMessage("Expiry must be after start date.");
    }
}

public sealed class CreateCouponCommandHandler : IRequestHandler<CreateCouponCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public CreateCouponCommandHandler(IApplicationDbContext db, ICurrentUserService user)
    { _db = db; _user = user; }

    public async Task<Result<Guid>> Handle(CreateCouponCommand req, CancellationToken ct)
    {
        var code = req.Code.Trim().ToUpperInvariant();
        if (await _db.Coupons.AnyAsync(c => c.Code == code, ct))
            return Result<Guid>.Failure($"Code '{code}' already exists.");

        // Vendor-scoped coupon — verify ownership
        if (req.VendorStoreId.HasValue)
        {
            var userId = _user.UserId ?? throw new ForbiddenAccessException();
            var owns = await _db.VendorStores
                .AnyAsync(s => s.Id == req.VendorStoreId.Value && s.Vendor.OwnerUserId == userId, ct);
            if (!owns) throw new ForbiddenAccessException();
        }

        var coupon = new Coupon
        {
            Code = code,
            DiscountType = req.DiscountType,
            DiscountValue = req.DiscountValue,
            MinimumOrderAmount = req.MinimumOrderAmount,
            MaxUses = req.MaxUses,
            MaxUsesPerCustomer = req.MaxUsesPerCustomer,
            StartsAtUtc = req.StartsAtUtc,
            ExpiresAtUtc = req.ExpiresAtUtc,
            IsActive = req.IsActive,
            VendorStoreId = req.VendorStoreId
        };
        _db.Coupons.Add(coupon);
        await _db.SaveChangesAsync(ct);
        return Result<Guid>.Success(coupon.Id);
    }
}

// ----- UPDATE -----
public sealed record UpdateCouponCommand(
    Guid Id,
    decimal DiscountValue,
    decimal MinimumOrderAmount,
    int? MaxUses,
    int? MaxUsesPerCustomer,
    DateTime? StartsAtUtc,
    DateTime? ExpiresAtUtc,
    bool IsActive
) : IRequest<Result>;

public sealed class UpdateCouponCommandValidator : AbstractValidator<UpdateCouponCommand>
{
    public UpdateCouponCommandValidator()
    {
        RuleFor(x => x.DiscountValue).GreaterThan(0);
        RuleFor(x => x.MinimumOrderAmount).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateCouponCommandHandler : IRequestHandler<UpdateCouponCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public UpdateCouponCommandHandler(IApplicationDbContext db, ICurrentUserService user)
    { _db = db; _user = user; }

    public async Task<Result> Handle(UpdateCouponCommand req, CancellationToken ct)
    {
        var coupon = await _db.Coupons
            .Include(c => c.VendorStore).ThenInclude(s => s!.Vendor)
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct);
        if (coupon is null) throw new NotFoundException(nameof(Coupon), req.Id);

        // Ownership: vendor coupons must belong to current user; platform coupons require admin role (enforced at controller).
        if (coupon.VendorStoreId.HasValue)
        {
            var userId = _user.UserId ?? throw new ForbiddenAccessException();
            if (coupon.VendorStore?.Vendor.OwnerUserId != userId
                && !_user.IsInRole("Admin"))
                throw new ForbiddenAccessException();
        }

        coupon.DiscountValue = req.DiscountValue;
        coupon.MinimumOrderAmount = req.MinimumOrderAmount;
        coupon.MaxUses = req.MaxUses;
        coupon.MaxUsesPerCustomer = req.MaxUsesPerCustomer;
        coupon.StartsAtUtc = req.StartsAtUtc;
        coupon.ExpiresAtUtc = req.ExpiresAtUtc;
        coupon.IsActive = req.IsActive;

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ----- DELETE -----
public sealed record DeleteCouponCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteCouponCommandHandler : IRequestHandler<DeleteCouponCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public DeleteCouponCommandHandler(IApplicationDbContext db, ICurrentUserService user)
    { _db = db; _user = user; }

    public async Task<Result> Handle(DeleteCouponCommand req, CancellationToken ct)
    {
        var coupon = await _db.Coupons
            .Include(c => c.VendorStore).ThenInclude(s => s!.Vendor)
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct);
        if (coupon is null) throw new NotFoundException(nameof(Coupon), req.Id);

        if (coupon.VendorStoreId.HasValue)
        {
            var userId = _user.UserId ?? throw new ForbiddenAccessException();
            if (coupon.VendorStore?.Vendor.OwnerUserId != userId
                && !_user.IsInRole("Admin"))
                throw new ForbiddenAccessException();
        }

        if (coupon.UsedCount > 0)
            return Result.Failure("Cannot delete a coupon that has been used. Deactivate it instead.");

        _db.Coupons.Remove(coupon);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
