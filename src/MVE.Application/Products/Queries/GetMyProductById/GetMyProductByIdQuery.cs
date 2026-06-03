using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Products.Queries.GetMyProductById;

public sealed record MyProductImageDto(Guid Id, string Url, bool IsMain, int SortOrder);
public sealed record MyProductVariantDto(Guid Id, string Sku, string? Name, string? Color, string? Size, decimal Price, int StockQuantity, bool IsActive);

public sealed record MyProductDetailDto(
    Guid Id,
    Guid VendorStoreId,
    string StoreName,
    Guid CategoryId,
    string CategoryName,
    string Name,
    string Slug,
    string? Description,
    decimal BasePrice,
    bool IsPublished,
    bool IsFeatured,
    ProductApprovalStatus ApprovalStatus,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    List<MyProductImageDto> Images,
    List<MyProductVariantDto> Variants
);

public sealed record GetMyProductByIdQuery(Guid Id) : IRequest<MyProductDetailDto>;

public sealed class GetMyProductByIdQueryHandler : IRequestHandler<GetMyProductByIdQuery, MyProductDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public GetMyProductByIdQueryHandler(IApplicationDbContext db, ICurrentUserService user)
    {
        _db = db; _user = user;
    }

    public async Task<MyProductDetailDto> Handle(GetMyProductByIdQuery req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();

        var product = await _db.Products
            .Where(p => p.Id == req.Id)
            .Select(p => new
            {
                p.Id, p.VendorStoreId, StoreName = p.VendorStore.Name, p.CategoryId,
                CategoryName = p.Category.Name,
                p.Name, p.Slug, p.Description, p.BasePrice,
                p.IsPublished, p.IsFeatured, p.ApprovalStatus,
                p.CreatedAtUtc, p.UpdatedAtUtc,
                OwnerUserId = p.VendorStore.Vendor.OwnerUserId,
                Images = p.Images.OrderBy(i => i.SortOrder)
                    .Select(i => new MyProductImageDto(i.Id, i.Url, i.IsMain, i.SortOrder)).ToList(),
                Variants = p.Variants
                    .Select(v => new MyProductVariantDto(v.Id, v.Sku, v.Name, v.Color, v.Size, v.Price, v.StockQuantity, v.IsActive)).ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (product is null) throw new NotFoundException(nameof(Domain.Entities.Product), req.Id);
        if (product.OwnerUserId != userId) throw new ForbiddenAccessException();

        return new MyProductDetailDto(
            product.Id, product.VendorStoreId, product.StoreName, product.CategoryId, product.CategoryName,
            product.Name, product.Slug, product.Description, product.BasePrice,
            product.IsPublished, product.IsFeatured, product.ApprovalStatus,
            product.CreatedAtUtc, product.UpdatedAtUtc, product.Images, product.Variants);
    }
}
