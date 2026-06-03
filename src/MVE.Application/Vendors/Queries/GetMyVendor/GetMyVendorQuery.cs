using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Vendors.Queries.GetMyVendor;

public sealed record MyVendorStoreDto(Guid Id, string Name, string? Slug, bool IsActive);
public sealed record MyVendorDto(
    Guid Id,
    string BusinessName,
    bool IsApproved,
    decimal DefaultCommissionPercent,
    List<MyVendorStoreDto> Stores
);

public sealed record GetMyVendorQuery() : IRequest<MyVendorDto?>;

public sealed class GetMyVendorQueryHandler : IRequestHandler<GetMyVendorQuery, MyVendorDto?>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public GetMyVendorQueryHandler(IApplicationDbContext db, ICurrentUserService user)
    {
        _db = db; _user = user;
    }

    public async Task<MyVendorDto?> Handle(GetMyVendorQuery req, CancellationToken ct)
    {
        var userId = _user.UserId;
        if (string.IsNullOrEmpty(userId)) return null;

        return await _db.Vendors
            .Where(v => v.OwnerUserId == userId)
            .Select(v => new MyVendorDto(
                v.Id, v.BusinessName, v.IsApproved, v.DefaultCommissionPercent,
                v.Stores.Select(s => new MyVendorStoreDto(s.Id, s.Name, s.Slug, s.IsActive)).ToList()))
            .FirstOrDefaultAsync(ct);
    }
}
