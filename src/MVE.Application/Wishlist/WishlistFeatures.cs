using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Wishlist;

public sealed record WishlistItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string ProductSlug,
    string? ImageUrl,
    decimal Price,
    bool InStock
);

public sealed record GetMyWishlistQuery() : IRequest<List<WishlistItemDto>>;

public sealed class GetMyWishlistQueryHandler : IRequestHandler<GetMyWishlistQuery, List<WishlistItemDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    public GetMyWishlistQueryHandler(IApplicationDbContext db, ICurrentUserService user) { _db = db; _user = user; }

    public async Task<List<WishlistItemDto>> Handle(GetMyWishlistQuery req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();
        return await _db.WishlistItems
            .Where(w => w.CustomerUserId == userId)
            .OrderByDescending(w => w.CreatedAtUtc)
            .Select(w => new WishlistItemDto(
                w.Id, w.ProductId, w.Product.Name, w.Product.Slug,
                w.Product.Images.OrderByDescending(i => i.IsMain).Select(i => i.Url).FirstOrDefault(),
                w.Product.BasePrice,
                w.Product.Variants.Any(v => v.IsActive && v.StockQuantity > 0)))
            .ToListAsync(ct);
    }
}

// ----- Add -----
public sealed record AddToWishlistCommand(Guid ProductId) : IRequest<Result>;

public sealed class AddToWishlistCommandHandler : IRequestHandler<AddToWishlistCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    public AddToWishlistCommandHandler(IApplicationDbContext db, ICurrentUserService user) { _db = db; _user = user; }

    public async Task<Result> Handle(AddToWishlistCommand req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();

        var exists = await _db.WishlistItems.AnyAsync(w => w.CustomerUserId == userId && w.ProductId == req.ProductId, ct);
        if (exists) return Result.Success();

        var product = await _db.Products
            .Where(p => p.Id == req.ProductId
                        && p.IsPublished
                        && p.ApprovalStatus == ProductApprovalStatus.Approved)
            .Select(p => p.Id).FirstOrDefaultAsync(ct);
        if (product == Guid.Empty) return Result.Failure("Product not available.");

        _db.WishlistItems.Add(new WishlistItem { CustomerUserId = userId, ProductId = req.ProductId });
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ----- Remove -----
public sealed record RemoveFromWishlistCommand(Guid Id) : IRequest<Result>;

public sealed class RemoveFromWishlistCommandHandler : IRequestHandler<RemoveFromWishlistCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    public RemoveFromWishlistCommandHandler(IApplicationDbContext db, ICurrentUserService user) { _db = db; _user = user; }

    public async Task<Result> Handle(RemoveFromWishlistCommand req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();
        var item = await _db.WishlistItems.FirstOrDefaultAsync(w => w.Id == req.Id, ct);
        if (item is null) return Result.Failure("Item not found.");
        if (item.CustomerUserId != userId) throw new ForbiddenAccessException();
        _db.WishlistItems.Remove(item);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
