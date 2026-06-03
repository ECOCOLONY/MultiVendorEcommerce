using Application.Common.Exceptions;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.ProductVariants.Queries.GetLowStockVariants;

public sealed record LowStockVariantDto(
    Guid VariantId,
    Guid ProductId,
    string ProductName,
    string ProductSlug,
    string Sku,
    string? Color,
    string? Size,
    int StockQuantity,
    bool IsActive
);

public sealed record GetLowStockVariantsQuery(int Threshold = 5) : IRequest<List<LowStockVariantDto>>;

public sealed class GetLowStockVariantsQueryHandler : IRequestHandler<GetLowStockVariantsQuery, List<LowStockVariantDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public GetLowStockVariantsQueryHandler(IApplicationDbContext db, ICurrentUserService user)
    { _db = db; _user = user; }

    public async Task<List<LowStockVariantDto>> Handle(GetLowStockVariantsQuery req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();

        return await _db.ProductVariants
            .Where(v => v.Product.VendorStore.Vendor.OwnerUserId == userId
                        && v.StockQuantity <= req.Threshold)
            .OrderBy(v => v.StockQuantity)
            .Select(v => new LowStockVariantDto(
                v.Id, v.ProductId, v.Product.Name, v.Product.Slug,
                v.Sku, v.Color, v.Size, v.StockQuantity, v.IsActive))
            .ToListAsync(ct);
    }
}
