using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Products.Queries.GetPublicProductBySlug;

public sealed record PublicProductImageDto(string Url, bool IsMain, int SortOrder);
public sealed record PublicProductVariantDto(Guid Id, string Sku, string? Name, string? Color, string? Size, decimal Price, int StockQuantity);

public sealed record PublicProductDetailDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    decimal BasePrice,
    decimal AverageRating,
    int ReviewCount,
    string CategoryName,
    Guid VendorStoreId,
    string StoreName,
    string? StoreSlug,
    List<PublicProductImageDto> Images,
    List<PublicProductVariantDto> Variants
);

public sealed record GetPublicProductBySlugQuery(string Slug) : IRequest<PublicProductDetailDto>;

public sealed class GetPublicProductBySlugQueryHandler : IRequestHandler<GetPublicProductBySlugQuery, PublicProductDetailDto>
{
    private readonly IApplicationDbContext _db;
    public GetPublicProductBySlugQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PublicProductDetailDto> Handle(GetPublicProductBySlugQuery req, CancellationToken ct)
    {
        var p = await _db.Products
            .Where(x => x.Slug == req.Slug
                        && x.IsPublished
                        && x.ApprovalStatus == ProductApprovalStatus.Approved
                        && x.VendorStore.IsActive)
            .Select(x => new PublicProductDetailDto(
                x.Id, x.Name, x.Slug, x.Description, x.BasePrice,
                x.AverageRating, x.ReviewCount,
                x.Category.Name,
                x.VendorStoreId, x.VendorStore.Name, x.VendorStore.Slug,
                x.Images.OrderBy(i => i.SortOrder)
                    .Select(i => new PublicProductImageDto(i.Url, i.IsMain, i.SortOrder)).ToList(),
                x.Variants.Where(v => v.IsActive)
                    .Select(v => new PublicProductVariantDto(v.Id, v.Sku, v.Name, v.Color, v.Size, v.Price, v.StockQuantity)).ToList()))
            .FirstOrDefaultAsync(ct);

        if (p is null) throw new NotFoundException("Product", req.Slug);
        return p;
    }
}
