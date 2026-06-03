using Application.Common.Exceptions;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.VendorStores.Queries.GetMyStore;

public sealed record MyStoreDto(
    Guid Id,
    Guid VendorId,
    string Name,
    string? Slug,
    string? Description,
    bool IsActive,
    string? LogoUrl,
    string? BannerUrl,
    string? ContactEmail,
    string? ContactPhone
);

public sealed record GetMyStoreQuery() : IRequest<MyStoreDto>;

public sealed class GetMyStoreQueryHandler : IRequestHandler<GetMyStoreQuery, MyStoreDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public GetMyStoreQueryHandler(IApplicationDbContext db, ICurrentUserService user)
    {
        _db = db;
        _user = user;
    }

    public async Task<MyStoreDto> Handle(GetMyStoreQuery req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();
        var store = await _db.VendorStores
            .Where(s => s.Vendor.OwnerUserId == userId)
            .OrderBy(s => s.CreatedAtUtc)
            .Select(s => new MyStoreDto(
                s.Id, s.VendorId, s.Name, s.Slug, s.Description, s.IsActive,
                s.LogoUrl, s.BannerUrl, s.ContactEmail, s.ContactPhone))
            .FirstOrDefaultAsync(ct);

        if (store is null) throw new NotFoundException("Store", "current user");
        return store;
    }
}
