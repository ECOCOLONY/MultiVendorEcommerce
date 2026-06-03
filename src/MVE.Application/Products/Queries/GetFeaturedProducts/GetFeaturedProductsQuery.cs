using Application.Common.Interfaces;
using Application.Products.Queries.GetPublicProducts;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Products.Queries.GetFeaturedProducts;

public sealed record GetFeaturedProductsQuery(int Take = 8) : IRequest<List<PublicProductCardDto>>;

public sealed class GetFeaturedProductsQueryHandler : IRequestHandler<GetFeaturedProductsQuery, List<PublicProductCardDto>>
{
    private readonly IApplicationDbContext _db;
    public GetFeaturedProductsQueryHandler(IApplicationDbContext db) => _db = db;

    public Task<List<PublicProductCardDto>> Handle(GetFeaturedProductsQuery req, CancellationToken ct) =>
        _db.Products
            .Where(p => p.IsPublished
                        && p.ApprovalStatus == ProductApprovalStatus.Approved
                        && p.VendorStore.IsActive)
            .OrderByDescending(p => p.IsFeatured)
            .ThenByDescending(p => p.AverageRating)
            .ThenByDescending(p => p.CreatedAtUtc)
            .Take(req.Take)
            .Select(p => new PublicProductCardDto(
                p.Id, p.Name, p.Slug,
                p.Images.OrderByDescending(i => i.IsMain).Select(i => i.Url).FirstOrDefault(),
                p.BasePrice,
                p.Variants.Where(v => v.IsActive).Min(v => (decimal?)v.Price),
                p.Category.Name,
                p.VendorStore.Name,
                p.VendorStore.Slug,
                p.AverageRating,
                p.ReviewCount))
            .ToListAsync(ct);
}
