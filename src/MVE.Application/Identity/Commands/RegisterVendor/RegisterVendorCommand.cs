using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Identity.Commands.RegisterVendor;

public sealed record RegisterVendorCommand(
    string FullName,
    string Email,
    string Password,
    string BusinessName,
    string StoreName,
    string? TaxNumber
) : IRequest<Result<string>>;

public sealed class RegisterVendorCommandValidator : AbstractValidator<RegisterVendorCommand>
{
    public RegisterVendorCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().Length(2, 120);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).MaximumLength(100);
        RuleFor(x => x.BusinessName).NotEmpty().Length(2, 200);
        RuleFor(x => x.StoreName).NotEmpty().Length(2, 200);
        RuleFor(x => x.TaxNumber).MaximumLength(50);
    }
}

public sealed class RegisterVendorCommandHandler : IRequestHandler<RegisterVendorCommand, Result<string>>
{
    private readonly IIdentityService _identity;
    private readonly IApplicationDbContext _db;

    public RegisterVendorCommandHandler(IIdentityService identity, IApplicationDbContext db)
    {
        _identity = identity;
        _db = db;
    }

    public async Task<Result<string>> Handle(RegisterVendorCommand req, CancellationToken ct)
    {
        var create = await _identity.CreateUserAsync(req.Email, req.Password, req.FullName, "Vendor");
        if (!create.Succeeded || create.Value is null) return create;

        var userId = create.Value;
        var vendor = new Vendor
        {
            OwnerUserId = userId,
            BusinessName = req.BusinessName,
            TaxNumber = req.TaxNumber,
            IsApproved = false,
            DefaultCommissionPercent = 10m
        };
        var store = new VendorStore
        {
            Vendor = vendor,
            Name = req.StoreName,
            Slug = SlugHelper.Slugify(req.StoreName),
            IsActive = false
        };
        _db.Vendors.Add(vendor);
        _db.VendorStores.Add(store);
        await _db.SaveChangesAsync(ct);

        return Result<string>.Success(userId);
    }
}

internal static class SlugHelper
{
    public static string Slugify(string input)
    {
        var s = input.Trim().ToLowerInvariant();
        var chars = s.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
        var collapsed = new string(chars);
        while (collapsed.Contains("--")) collapsed = collapsed.Replace("--", "-");
        return collapsed.Trim('-');
    }
}
