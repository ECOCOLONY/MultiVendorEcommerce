using Application.Addresses.Queries.GetMyAddresses;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Addresses.Queries.GetMyAddressById;

public sealed record GetMyAddressByIdQuery(Guid Id) : IRequest<AddressDto>;

public sealed class GetMyAddressByIdQueryHandler : IRequestHandler<GetMyAddressByIdQuery, AddressDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public GetMyAddressByIdQueryHandler(IApplicationDbContext db, ICurrentUserService user)
    {
        _db = db; _user = user;
    }

    public async Task<AddressDto> Handle(GetMyAddressByIdQuery req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();

        var a = await _db.Addresses
            .Where(a => a.Id == req.Id && a.UserId == userId)
            .Select(a => new AddressDto(a.Id, a.Label, a.Line1, a.Line2, a.City, a.State,
                a.PostalCode, a.Country, a.Phone, a.IsDefault))
            .FirstOrDefaultAsync(ct);

        if (a is null) throw new NotFoundException(nameof(Domain.Entities.Address), req.Id);
        return a;
    }
}
