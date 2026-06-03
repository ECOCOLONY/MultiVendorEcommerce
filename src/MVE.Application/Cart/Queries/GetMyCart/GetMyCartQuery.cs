using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Cart.Queries.GetMyCart;

public sealed record CartItemDto(
    Guid Id,
    Guid ProductVariantId,
    Guid ProductId,
    string ProductName,
    string ProductSlug,
    string? VariantSku,
    string? VariantColor,
    string? VariantSize,
    string? ImageUrl,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    int StockAvailable,
    string StoreName
);

public sealed record CartDto(
    Guid CartId,
    List<CartItemDto> Items,
    decimal Subtotal,
    int ItemCount
);

public sealed record GetMyCartQuery() : IRequest<CartDto>;

public sealed class GetMyCartQueryHandler : IRequestHandler<GetMyCartQuery, CartDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public GetMyCartQueryHandler(IApplicationDbContext db, ICurrentUserService user)
    {
        _db = db; _user = user;
    }

    public async Task<CartDto> Handle(GetMyCartQuery req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();

        var cart = await _db.Carts
            .Include(c => c.Items).ThenInclude(i => i.ProductVariant).ThenInclude(v => v.Product).ThenInclude(p => p.VendorStore)
            .Include(c => c.Items).ThenInclude(i => i.ProductVariant).ThenInclude(v => v.Product).ThenInclude(p => p.Images)
            .FirstOrDefaultAsync(c => c.CustomerUserId == userId, ct);

        if (cart is null)
        {
            cart = new Domain.Entities.Cart { CustomerUserId = userId };
            _db.Carts.Add(cart);
            await _db.SaveChangesAsync(ct);
            return new CartDto(cart.Id, new List<CartItemDto>(), 0m, 0);
        }

        var items = cart.Items.Select(i => new CartItemDto(
            i.Id,
            i.ProductVariantId,
            i.ProductVariant.ProductId,
            i.ProductVariant.Product.Name,
            i.ProductVariant.Product.Slug,
            i.ProductVariant.Sku,
            i.ProductVariant.Color,
            i.ProductVariant.Size,
            i.ProductVariant.Product.Images.OrderByDescending(im => im.IsMain).Select(im => im.Url).FirstOrDefault(),
            i.Quantity,
            i.UnitPrice,
            i.UnitPrice * i.Quantity,
            i.ProductVariant.StockQuantity,
            i.ProductVariant.Product.VendorStore.Name
        )).ToList();

        return new CartDto(cart.Id, items, items.Sum(i => i.LineTotal), items.Sum(i => i.Quantity));
    }
}
