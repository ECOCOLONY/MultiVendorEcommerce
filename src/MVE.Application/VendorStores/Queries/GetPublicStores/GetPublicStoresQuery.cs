using Application.Common.Interfaces;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.VendorStores.Queries.GetPublicStores;

public sealed record PublicStoreCardDto(
    Guid Id,
    string Name,
    string? Slug,
    string? LogoUrl,
    string? BannerUrl,
    int ProductCount,
    string VendorBusiness
);

public sealed record GetPublicStoresQuery() : IRequest<List<PublicStoreCardDto>>;

public sealed class GetPublicStoresQueryHandler : IRequestHandler<GetPublicStoresQuery, List<PublicStoreCardDto>>
{
    private readonly IApplicationDbContext _db;
    public GetPublicStoresQueryHandler(IApplicationDbContext db) => _db = db;

    public Task<List<PublicStoreCardDto>> Handle(GetPublicStoresQuery req, CancellationToken ct) =>
        _db.VendorStores
            .Where(s => s.IsActive && s.Vendor.IsApproved)
            .OrderBy(s => s.Name)
            .Select(s => new PublicStoreCardDto(
                s.Id, s.Name, s.Slug, s.LogoUrl, s.BannerUrl,
                s.Products.Count(p => p.IsPublished && p.ApprovalStatus == ProductApprovalStatus.Approved),
                s.Vendor.BusinessName))
            .ToListAsync(ct);
}
