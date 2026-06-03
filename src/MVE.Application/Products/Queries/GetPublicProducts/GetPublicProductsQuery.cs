using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Products.Queries.GetPublicProducts;

public sealed record PublicProductCardDto(
    Guid Id,
    string Name,
    string Slug,
    string? MainImageUrl,
    decimal BasePrice,
    decimal? MinVariantPrice,
    string CategoryName,
    string StoreName,
    string? StoreSlug,
    decimal AverageRating,
    int ReviewCount
);

public sealed record GetPublicProductsQuery(
    string? Search = null,
    Guid? CategoryId = null,
    string? StoreSlug = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    int Page = 1,
    int PageSize = 12
) : IRequest<PaginatedList<PublicProductCardDto>>;

public sealed class GetPublicProductsQueryHandler
    : IRequestHandler<GetPublicProductsQuery, PaginatedList<PublicProductCardDto>>
{
    private readonly IApplicationDbContext _db;
    public GetPublicProductsQueryHandler(IApplicationDbContext db) => _db = db;

    public Task<PaginatedList<PublicProductCardDto>> Handle(GetPublicProductsQuery req, CancellationToken ct)
    {
        var q = _db.Products
            .Where(p => p.IsPublished
                        && p.ApprovalStatus == ProductApprovalStatus.Approved
                        && p.VendorStore.IsActive);

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.Trim();
            q = q.Where(p => p.Name.Contains(s) || (p.Description != null && p.Description.Contains(s)));
        }
        if (req.CategoryId.HasValue) q = q.Where(p => p.CategoryId == req.CategoryId.Value);
        if (!string.IsNullOrWhiteSpace(req.StoreSlug)) q = q.Where(p => p.VendorStore.Slug == req.StoreSlug);
        if (req.MinPrice.HasValue) q = q.Where(p => p.BasePrice >= req.MinPrice.Value);
        if (req.MaxPrice.HasValue) q = q.Where(p => p.BasePrice <= req.MaxPrice.Value);

        var projected = q
            .OrderByDescending(p => p.IsFeatured)
            .ThenByDescending(p => p.AverageRating)
            .ThenByDescending(p => p.CreatedAtUtc)
            .Select(p => new PublicProductCardDto(
                p.Id, p.Name, p.Slug,
                p.Images.OrderByDescending(i => i.IsMain).Select(i => i.Url).FirstOrDefault(),
                p.BasePrice,
                p.Variants.Where(v => v.IsActive).Min(v => (decimal?)v.Price),
                p.Category.Name,
                p.VendorStore.Name,
                p.VendorStore.Slug,
                p.AverageRating,
                p.ReviewCount));

        return PaginatedList<PublicProductCardDto>.CreateAsync(projected, req.Page, req.PageSize, ct);
    }
}
