using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Products.Commands.CreateProduct;

public sealed record CreateVariantInput(string Sku, string? Name, string? Color, string? Size, decimal Price, int StockQuantity);

public sealed record CreateProductCommand(
    Guid VendorStoreId,
    Guid CategoryId,
    string Name,
    string? Description,
    decimal BasePrice,
    List<string> ImageUrls,
    List<CreateVariantInput> Variants
) : IRequest<Result<Guid>>;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().Length(2, 200);
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.BasePrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.VendorStoreId).NotEmpty();
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Variants).NotEmpty().WithMessage("At least one variant is required.");
        RuleForEach(x => x.Variants).ChildRules(v =>
        {
            v.RuleFor(x => x.Sku).NotEmpty().MaximumLength(80);
            v.RuleFor(x => x.Price).GreaterThan(0);
            v.RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0);
        });
    }
}

public sealed class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public CreateProductCommandHandler(IApplicationDbContext db, ICurrentUserService user)
    {
        _db = db; _user = user;
    }

    public async Task<Result<Guid>> Handle(CreateProductCommand req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();

        var store = await _db.VendorStores
            .Include(s => s.Vendor)
            .FirstOrDefaultAsync(s => s.Id == req.VendorStoreId, ct);
        if (store is null) return Result<Guid>.Failure("Store not found.");
        if (store.Vendor.OwnerUserId != userId) throw new ForbiddenAccessException();
        if (!store.Vendor.IsApproved) return Result<Guid>.Failure("Your vendor account is not yet approved.");

        var skus = req.Variants.Select(v => v.Sku).ToArray();
        var existsSku = await _db.ProductVariants.AnyAsync(v => skus.Contains(v.Sku), ct);
        if (existsSku) return Result<Guid>.Failure("One of the SKUs is already in use.");

        var slug = SlugHelper.Make(req.Name);
        var slugConflict = await _db.Products.AnyAsync(p => p.Slug == slug, ct);
        if (slugConflict) slug = $"{slug}-{Guid.NewGuid().ToString("N")[..6]}";

        var product = new Product
        {
            VendorStoreId  = req.VendorStoreId,
            CategoryId     = req.CategoryId,
            Name           = req.Name.Trim(),
            Slug           = slug,
            Description    = req.Description?.Trim(),
            BasePrice      = req.BasePrice,
            IsPublished    = false,
            ApprovalStatus = ProductApprovalStatus.Pending,
            Images = req.ImageUrls.Select((url, i) => new ProductImage
            {
                Url = url,
                IsMain = i == 0,
                SortOrder = i
            }).ToList(),
            Variants = req.Variants.Select(v => new ProductVariant
            {
                Sku = v.Sku.Trim(),
                Name = v.Name?.Trim(),
                Color = v.Color?.Trim(),
                Size = v.Size?.Trim(),
                Price = v.Price,
                StockQuantity = v.StockQuantity,
                IsActive = true
            }).ToList()
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);
        return Result<Guid>.Success(product.Id);
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
