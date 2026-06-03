using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Enums;
using MediatR;

namespace Application.Products.Queries.GetProductsForAdmin;

public sealed record AdminProductListItemDto(
    Guid Id,
    string Name,
    string Slug,
    string? MainImageUrl,
    decimal BasePrice,
    string StoreName,
    string VendorBusiness,
    string CategoryName,
    bool IsPublished,
    ProductApprovalStatus ApprovalStatus,
    DateTime CreatedAtUtc
);

public sealed record GetProductsForAdminQuery(
    ProductApprovalStatus? Status = null,
    string? Search = null,
    Guid? CategoryId = null,
    string? VendorSearch = null,
    int Page = 1,
    int PageSize = 15
) : IRequest<PaginatedList<AdminProductListItemDto>>;

public sealed class GetProductsForAdminQueryHandler : IRequestHandler<GetProductsForAdminQuery, PaginatedList<AdminProductListItemDto>>
{
    private readonly IApplicationDbContext _db;
    public GetProductsForAdminQueryHandler(IApplicationDbContext db) => _db = db;

    public Task<PaginatedList<AdminProductListItemDto>> Handle(GetProductsForAdminQuery req, CancellationToken ct)
    {
        var q = _db.Products.AsQueryable();
        if (req.Status.HasValue) q = q.Where(p => p.ApprovalStatus == req.Status.Value);
        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.Trim();
            q = q.Where(p => p.Name.Contains(s));
        }
        if (req.CategoryId.HasValue) q = q.Where(p => p.CategoryId == req.CategoryId.Value);
        if (!string.IsNullOrWhiteSpace(req.VendorSearch))
        {
            var vs = req.VendorSearch.Trim();
            q = q.Where(p => p.VendorStore.Vendor.BusinessName.Contains(vs) || p.VendorStore.Name.Contains(vs));
        }

        var projection = q
            .OrderBy(p => p.ApprovalStatus == ProductApprovalStatus.Pending ? 0 : 1)
            .ThenByDescending(p => p.CreatedAtUtc)
            .Select(p => new AdminProductListItemDto(
                p.Id, p.Name, p.Slug,
                p.Images.OrderByDescending(i => i.IsMain).Select(i => i.Url).FirstOrDefault(),
                p.BasePrice,
                p.VendorStore.Name,
                p.VendorStore.Vendor.BusinessName,
                p.Category.Name,
                p.IsPublished, p.ApprovalStatus,
                p.CreatedAtUtc));

        return PaginatedList<AdminProductListItemDto>.CreateAsync(projection, req.Page, req.PageSize, ct);
    }
}
