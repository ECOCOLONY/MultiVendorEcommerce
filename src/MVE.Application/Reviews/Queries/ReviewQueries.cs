using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Reviews.Queries;

public sealed record PublicReviewDto(
    Guid Id,
    int Rating,
    string? Title,
    string? Comment,
    bool VerifiedPurchase,
    DateTime CreatedAtUtc,
    string? VendorReply,
    DateTime? VendorRepliedAtUtc
);

public sealed record GetPublicReviewsQuery(Guid ProductId) : IRequest<List<PublicReviewDto>>;

public sealed class GetPublicReviewsQueryHandler : IRequestHandler<GetPublicReviewsQuery, List<PublicReviewDto>>
{
    private readonly IApplicationDbContext _db;
    public GetPublicReviewsQueryHandler(IApplicationDbContext db) => _db = db;

    public Task<List<PublicReviewDto>> Handle(GetPublicReviewsQuery req, CancellationToken ct) =>
        _db.Reviews
            .Where(r => r.ProductId == req.ProductId && r.IsApproved)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Select(r => new PublicReviewDto(
                r.Id, r.Rating, r.Title, r.Comment,
                r.OrderItemId.HasValue,
                r.CreatedAtUtc,
                r.VendorReply, r.VendorRepliedAtUtc))
            .ToListAsync(ct);
}

// ----- Vendor reviews list -----
public sealed record VendorReviewDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    int Rating,
    string? Title,
    string? Comment,
    bool IsApproved,
    DateTime CreatedAtUtc,
    string? VendorReply,
    DateTime? VendorRepliedAtUtc
);

public sealed record GetVendorReviewsQuery(
    bool? IsApproved = null,
    int? Rating = null,
    string? ProductSearch = null,
    int Page = 1,
    int PageSize = 15
) : IRequest<PaginatedList<VendorReviewDto>>;

public sealed class GetVendorReviewsQueryHandler : IRequestHandler<GetVendorReviewsQuery, PaginatedList<VendorReviewDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    public GetVendorReviewsQueryHandler(IApplicationDbContext db, ICurrentUserService user) { _db = db; _user = user; }

    public Task<PaginatedList<VendorReviewDto>> Handle(GetVendorReviewsQuery req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();
        var q = _db.Reviews.Where(r => r.Product.VendorStore.Vendor.OwnerUserId == userId);
        if (req.IsApproved.HasValue) q = q.Where(r => r.IsApproved == req.IsApproved.Value);
        if (req.Rating.HasValue) q = q.Where(r => r.Rating == req.Rating.Value);
        if (!string.IsNullOrWhiteSpace(req.ProductSearch))
        {
            var s = req.ProductSearch.Trim();
            q = q.Where(r => r.Product.Name.Contains(s));
        }

        var projection = q
            .OrderByDescending(r => r.CreatedAtUtc)
            .Select(r => new VendorReviewDto(
                r.Id, r.ProductId, r.Product.Name,
                r.Rating, r.Title, r.Comment,
                r.IsApproved, r.CreatedAtUtc,
                r.VendorReply, r.VendorRepliedAtUtc));

        return PaginatedList<VendorReviewDto>.CreateAsync(projection, req.Page, req.PageSize, ct);
    }
}

// ----- Admin reviews list (moderation) -----
public sealed record AdminReviewDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string CustomerUserId,
    int Rating,
    string? Title,
    string? Comment,
    bool IsApproved,
    DateTime CreatedAtUtc
);

public sealed record GetAdminReviewsQuery(
    bool? IsApproved = null,
    int? Rating = null,
    string? ProductSearch = null,
    int Page = 1,
    int PageSize = 15
) : IRequest<PaginatedList<AdminReviewDto>>;

public sealed class GetAdminReviewsQueryHandler : IRequestHandler<GetAdminReviewsQuery, PaginatedList<AdminReviewDto>>
{
    private readonly IApplicationDbContext _db;
    public GetAdminReviewsQueryHandler(IApplicationDbContext db) => _db = db;

    public Task<PaginatedList<AdminReviewDto>> Handle(GetAdminReviewsQuery req, CancellationToken ct)
    {
        var q = _db.Reviews.AsQueryable();
        if (req.IsApproved.HasValue) q = q.Where(r => r.IsApproved == req.IsApproved.Value);
        if (req.Rating.HasValue) q = q.Where(r => r.Rating == req.Rating.Value);
        if (!string.IsNullOrWhiteSpace(req.ProductSearch))
        {
            var s = req.ProductSearch.Trim();
            q = q.Where(r => r.Product.Name.Contains(s));
        }

        var projection = q
            .OrderBy(r => r.IsApproved ? 1 : 0)
            .ThenByDescending(r => r.CreatedAtUtc)
            .Select(r => new AdminReviewDto(
                r.Id, r.ProductId, r.Product.Name, r.CustomerUserId,
                r.Rating, r.Title, r.Comment, r.IsApproved, r.CreatedAtUtc));

        return PaginatedList<AdminReviewDto>.CreateAsync(projection, req.Page, req.PageSize, ct);
    }
}
