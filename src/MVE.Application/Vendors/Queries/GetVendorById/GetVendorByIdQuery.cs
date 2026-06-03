using Application.Common.Exceptions;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Vendors.Queries.GetVendorById;

public sealed record VendorStoreDto(
    Guid Id,
    string Name,
    string? Slug,
    bool IsActive,
    int ProductCount
);

public sealed record VendorDetailDto(
    Guid Id,
    string OwnerUserId,
    string BusinessName,
    string? TaxNumber,
    bool IsApproved,
    decimal DefaultCommissionPercent,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    List<VendorStoreDto> Stores
);

public sealed record GetVendorByIdQuery(Guid Id) : IRequest<VendorDetailDto>;

public sealed class GetVendorByIdQueryHandler : IRequestHandler<GetVendorByIdQuery, VendorDetailDto>
{
    private readonly IApplicationDbContext _db;
    public GetVendorByIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<VendorDetailDto> Handle(GetVendorByIdQuery req, CancellationToken ct)
    {
        var v = await _db.Vendors
            .Where(x => x.Id == req.Id)
            .Select(x => new VendorDetailDto(
                x.Id, x.OwnerUserId, x.BusinessName, x.TaxNumber,
                x.IsApproved, x.DefaultCommissionPercent,
                x.CreatedAtUtc, x.UpdatedAtUtc,
                x.Stores.Select(s => new VendorStoreDto(s.Id, s.Name, s.Slug, s.IsActive, s.Products.Count)).ToList()))
            .FirstOrDefaultAsync(ct);

        if (v is null) throw new NotFoundException(nameof(Domain.Entities.Vendor), req.Id);
        return v;
    }
}
