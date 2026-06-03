using Application.Common.Interfaces;
using Application.Common.Models;
using MediatR;

namespace Application.Users.Queries.GetUsersList;

public sealed record GetUsersListQuery(
    string? Search = null,
    string? Role = null,
    int Page = 1,
    int PageSize = 15
) : IRequest<PaginatedList<UserSummary>>;

public sealed class GetUsersListQueryHandler : IRequestHandler<GetUsersListQuery, PaginatedList<UserSummary>>
{
    private readonly IIdentityService _identity;
    public GetUsersListQueryHandler(IIdentityService identity) => _identity = identity;

    public Task<PaginatedList<UserSummary>> Handle(GetUsersListQuery req, CancellationToken ct) =>
        _identity.GetUsersPagedAsync(req.Search, req.Role, req.Page, req.PageSize, ct);
}
