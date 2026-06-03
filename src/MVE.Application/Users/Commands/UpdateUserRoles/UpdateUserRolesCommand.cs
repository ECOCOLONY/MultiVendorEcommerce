using Application.Common.Interfaces;
using Application.Common.Models;
using MediatR;

namespace Application.Users.Commands.UpdateUserRoles;

public sealed record UpdateUserRolesCommand(string UserId, IEnumerable<string> Roles) : IRequest<Result>;

public sealed class UpdateUserRolesCommandHandler : IRequestHandler<UpdateUserRolesCommand, Result>
{
    private readonly IIdentityService _identity;
    public UpdateUserRolesCommandHandler(IIdentityService identity) => _identity = identity;

    public Task<Result> Handle(UpdateUserRolesCommand req, CancellationToken ct) =>
        _identity.SetUserRolesAsync(req.UserId, req.Roles);
}
