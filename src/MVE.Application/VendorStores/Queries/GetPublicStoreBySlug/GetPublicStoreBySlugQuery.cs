using Application.Common.Exceptions;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.VendorStores.Queries.GetPublicStoreBySlug;

public sealed record PublicStoreDto(
    Guid Id,
    string Name,
    string? Slug,
    string? Description,
    string? LogoUrl,
    string? BannerUrl,
    string? ContactEmail,
    string? ContactPhone,
    string VendorBusiness,
    int ProductCount
);

public sealed record GetPublicStoreBySlugQuery(string Slug) : IRequest<PublicStoreDto>;

public sealed class GetPublicStoreBySlugQueryHandler : IRequestHandler<GetPublicStoreBySlugQuery, PublicStoreDto>
{
    private readonly IApplicationDbContext _db;
    public GetPublicStoreBySlugQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PublicStoreDto> Handle(GetPublicStoreBySlugQuery req, CancellationToken ct)
    {
        var s = await _db.VendorStores
            .Where(x => x.Slug == req.Slug && x.IsActive && x.Vendor.IsApproved)
            .Select(x => new PublicStoreDto(
                x.Id, x.Name, x.Slug, x.Description, x.LogoUrl, x.BannerUrl, x.ContactEmail, x.ContactPhone,
                x.Vendor.BusinessName,
                x.Products.Count(p => p.IsPublished
                                       && p.ApprovalStatus == Domain.Enums.ProductApprovalStatus.Approved)))
            .FirstOrDefaultAsync(ct);

        if (s is null) throw new NotFoundException("Store", req.Slug);
        return s;
    }
}
