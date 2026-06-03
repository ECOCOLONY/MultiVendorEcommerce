using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.VendorStores.Commands.UpdateMyStore;

public sealed record UpdateMyStoreCommand(
    Guid StoreId,
    string Name,
    string? Description,
    string? ContactEmail,
    string? ContactPhone,
    string? LogoUrl,
    string? BannerUrl
) : IRequest<Result>;

public sealed class UpdateMyStoreCommandValidator : AbstractValidator<UpdateMyStoreCommand>
{
    public UpdateMyStoreCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().Length(2, 200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.ContactEmail).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.ContactEmail));
        RuleFor(x => x.ContactPhone).MaximumLength(50);
    }
}

public sealed class UpdateMyStoreCommandHandler : IRequestHandler<UpdateMyStoreCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public UpdateMyStoreCommandHandler(IApplicationDbContext db, ICurrentUserService user)
    {
        _db = db; _user = user;
    }

    public async Task<Result> Handle(UpdateMyStoreCommand req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();
        var store = await _db.VendorStores
            .Include(s => s.Vendor)
            .FirstOrDefaultAsync(s => s.Id == req.StoreId, ct);

        if (store is null) throw new NotFoundException(nameof(Domain.Entities.VendorStore), req.StoreId);
        if (store.Vendor.OwnerUserId != userId) throw new ForbiddenAccessException();

        store.Name         = req.Name.Trim();
        store.Slug         = SlugHelper.Make(req.Name);
        store.Description  = req.Description?.Trim();
        store.ContactEmail = req.ContactEmail?.Trim();
        store.ContactPhone = req.ContactPhone?.Trim();
        if (req.LogoUrl is not null)   store.LogoUrl = req.LogoUrl;
        if (req.BannerUrl is not null) store.BannerUrl = req.BannerUrl;

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

internal static class SlugHelper
{
    public static string Make(string input)
    {
        var s = input.Trim().ToLowerInvariant();
        var chars = s.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
        var collapsed = new string(chars);
        while (collapsed.Contains("--")) collapsed = collapsed.Replace("--", "-");
        return collapsed.Trim('-');
    }
}
