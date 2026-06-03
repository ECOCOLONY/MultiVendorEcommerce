using Application.Common.Exceptions;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Users.Queries.GetUserById;

public sealed record GetUserByIdQuery(string Id) : IRequest<UserSummary>;

public sealed class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserSummary>
{
    private readonly IIdentityService _identity;
    public GetUserByIdQueryHandler(IIdentityService identity) => _identity = identity;

    public async Task<UserSummary> Handle(GetUserByIdQuery req, CancellationToken ct)
    {
        var u = await _identity.GetUserAsync(req.Id);
        if (u is null) throw new NotFoundException("User", req.Id);
        return u;
    }
}
