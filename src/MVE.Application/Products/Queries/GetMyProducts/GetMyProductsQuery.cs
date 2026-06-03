using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Enums;
using MediatR;

namespace Application.Products.Queries.GetMyProducts;

public sealed record MyProductListItemDto(
    Guid Id,
    string Name,
    string Slug,
    string? MainImageUrl,
    decimal BasePrice,
    int VariantCount,
    int TotalStock,
    bool IsPublished,
    ProductApprovalStatus ApprovalStatus,
    string CategoryName,
    string StoreName
);

public sealed record GetMyProductsQuery(
    string? Search = null,
    ProductApprovalStatus? ApprovalStatus = null,
    Guid? CategoryId = null,
    int Page = 1,
    int PageSize = 15
) : IRequest<PaginatedList<MyProductListItemDto>>;

public sealed class GetMyProductsQueryHandler : IRequestHandler<GetMyProductsQuery, PaginatedList<MyProductListItemDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public GetMyProductsQueryHandler(IApplicationDbContext db, ICurrentUserService user)
    {
        _db = db; _user = user;
    }

    public Task<PaginatedList<MyProductListItemDto>> Handle(GetMyProductsQuery req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();

        var q = _db.Products.Where(p => p.VendorStore.Vendor.OwnerUserId == userId);
        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.Trim();
            q = q.Where(p => p.Name.Contains(s));
        }
        if (req.ApprovalStatus.HasValue) q = q.Where(p => p.ApprovalStatus == req.ApprovalStatus.Value);
        if (req.CategoryId.HasValue)     q = q.Where(p => p.CategoryId == req.CategoryId.Value);

        var projection = q
            .OrderByDescending(p => p.CreatedAtUtc)
            .Select(p => new MyProductListItemDto(
                p.Id, p.Name, p.Slug,
                p.Images.OrderByDescending(i => i.IsMain).Select(i => i.Url).FirstOrDefault(),
                p.BasePrice,
                p.Variants.Count,
                p.Variants.Sum(v => v.StockQuantity),
                p.IsPublished, p.ApprovalStatus,
                p.Category.Name,
                p.VendorStore.Name));

        return PaginatedList<MyProductListItemDto>.CreateAsync(projection, req.Page, req.PageSize, ct);
    }
}
