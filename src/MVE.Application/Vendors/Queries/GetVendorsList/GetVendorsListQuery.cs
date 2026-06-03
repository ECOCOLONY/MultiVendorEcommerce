using Application.Common.Interfaces;
using Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Vendors.Queries.GetVendorsList;

public sealed record VendorListItemDto(
    Guid Id,
    string OwnerUserId,
    string BusinessName,
    string? TaxNumber,
    bool IsApproved,
    decimal DefaultCommissionPercent,
    int StoreCount,
    int ProductCount,
    DateTime CreatedAtUtc
);

public sealed record GetVendorsListQuery(
    bool? Approved = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 15
) : IRequest<PaginatedList<VendorListItemDto>>;

public sealed class GetVendorsListQueryHandler : IRequestHandler<GetVendorsListQuery, PaginatedList<VendorListItemDto>>
{
    private readonly IApplicationDbContext _db;
    public GetVendorsListQueryHandler(IApplicationDbContext db) => _db = db;

    public Task<PaginatedList<VendorListItemDto>> Handle(GetVendorsListQuery req, CancellationToken ct)
    {
        var q = _db.Vendors.AsQueryable();
        if (req.Approved.HasValue) q = q.Where(v => v.IsApproved == req.Approved.Value);
        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.Trim();
            q = q.Where(v => v.BusinessName.Contains(s));
        }

        var projection = q
            .OrderByDescending(v => v.CreatedAtUtc)
            .Select(v => new VendorListItemDto(
                v.Id, v.OwnerUserId, v.BusinessName, v.TaxNumber,
                v.IsApproved, v.DefaultCommissionPercent,
                v.Stores.Count,
                v.Stores.SelectMany(s => s.Products).Count(),
                v.CreatedAtUtc));

        return PaginatedList<VendorListItemDto>.CreateAsync(projection, req.Page, req.PageSize, ct);
    }
}
