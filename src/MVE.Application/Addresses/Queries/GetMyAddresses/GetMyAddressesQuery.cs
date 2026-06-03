using Application.Common.Exceptions;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Addresses.Queries.GetMyAddresses;

public sealed record AddressDto(
    Guid Id,
    string? Label,
    string Line1,
    string? Line2,
    string City,
    string? State,
    string PostalCode,
    string Country,
    string? Phone,
    bool IsDefault
);

public sealed record GetMyAddressesQuery() : IRequest<List<AddressDto>>;

public sealed class GetMyAddressesQueryHandler : IRequestHandler<GetMyAddressesQuery, List<AddressDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public GetMyAddressesQueryHandler(IApplicationDbContext db, ICurrentUserService user)
    {
        _db = db; _user = user;
    }

    public async Task<List<AddressDto>> Handle(GetMyAddressesQuery req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();

        return await _db.Addresses
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault).ThenBy(a => a.Label)
            .Select(a => new AddressDto(a.Id, a.Label, a.Line1, a.Line2, a.City, a.State,
                a.PostalCode, a.Country, a.Phone, a.IsDefault))
            .ToListAsync(ct);
    }
}
