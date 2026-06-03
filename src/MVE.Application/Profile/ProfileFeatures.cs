using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using FluentValidation;
using MediatR;

namespace Application.Profile;

public sealed record ProfileDto(string UserId, string Email, string FullName, string? ProfileImageUrl, IReadOnlyCollection<string> Roles);

public sealed record GetMyProfileQuery() : IRequest<ProfileDto>;

public sealed class GetMyProfileQueryHandler : IRequestHandler<GetMyProfileQuery, ProfileDto>
{
    private readonly IIdentityService _identity;
    private readonly ICurrentUserService _user;

    public GetMyProfileQueryHandler(IIdentityService identity, ICurrentUserService user)
    { _identity = identity; _user = user; }

    public async Task<ProfileDto> Handle(GetMyProfileQuery req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();
        var u = await _identity.GetUserAsync(userId);
        if (u is null) throw new NotFoundException("User", userId);
        return new ProfileDto(u.Id, u.Email, u.FullName, null, u.Roles);
    }
}

// ----- update profile -----
public sealed record UpdateProfileCommand(string FullName, string? ProfileImageUrl) : IRequest<Result>;

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().Length(2, 120);
        RuleFor(x => x.ProfileImageUrl).MaximumLength(500);
    }
}

public sealed class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, Result>
{
    private readonly IIdentityService _identity;
    private readonly ICurrentUserService _user;

    public UpdateProfileCommandHandler(IIdentityService identity, ICurrentUserService user)
    { _identity = identity; _user = user; }

    public Task<Result> Handle(UpdateProfileCommand req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();
        return _identity.UpdateProfileAsync(userId, req.FullName, req.ProfileImageUrl);
    }
}

// ----- change password -----
public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest<Result>;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(6).MaximumLength(100);
    }
}

public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
{
    private readonly IIdentityService _identity;
    private readonly ICurrentUserService _user;

    public ChangePasswordCommandHandler(IIdentityService identity, ICurrentUserService user)
    { _identity = identity; _user = user; }

    public Task<Result> Handle(ChangePasswordCommand req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();
        return _identity.ChangePasswordAsync(userId, req.CurrentPassword, req.NewPassword);
    }
}
