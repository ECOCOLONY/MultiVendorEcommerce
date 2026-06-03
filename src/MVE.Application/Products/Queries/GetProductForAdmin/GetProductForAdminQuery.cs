using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Products.Queries.GetProductForAdmin;

public sealed record AdminProductImageDto(Guid Id, string Url, bool IsMain);
public sealed record AdminProductVariantDto(Guid Id, string Sku, string? Color, string? Size, decimal Price, int StockQuantity, bool IsActive);

public sealed record AdminProductDetailDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    decimal BasePrice,
    bool IsPublished,
    ProductApprovalStatus ApprovalStatus,
    string StoreName,
    string VendorBusiness,
    string CategoryName,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    List<AdminProductImageDto> Images,
    List<AdminProductVariantDto> Variants
);

public sealed record GetProductForAdminQuery(Guid Id) : IRequest<AdminProductDetailDto>;

public sealed class GetProductForAdminQueryHandler : IRequestHandler<GetProductForAdminQuery, AdminProductDetailDto>
{
    private readonly IApplicationDbContext _db;
    public GetProductForAdminQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<AdminProductDetailDto> Handle(GetProductForAdminQuery req, CancellationToken ct)
    {
        var p = await _db.Products
            .Where(x => x.Id == req.Id)
            .Select(x => new AdminProductDetailDto(
                x.Id, x.Name, x.Slug, x.Description, x.BasePrice,
                x.IsPublished, x.ApprovalStatus,
                x.VendorStore.Name, x.VendorStore.Vendor.BusinessName, x.Category.Name,
                x.CreatedAtUtc, x.UpdatedAtUtc,
                x.Images.OrderBy(i => i.SortOrder).Select(i => new AdminProductImageDto(i.Id, i.Url, i.IsMain)).ToList(),
                x.Variants.Select(v => new AdminProductVariantDto(v.Id, v.Sku, v.Color, v.Size, v.Price, v.StockQuantity, v.IsActive)).ToList()))
            .FirstOrDefaultAsync(ct);

        if (p is null) throw new NotFoundException(nameof(Domain.Entities.Product), req.Id);
        return p;
    }
}
