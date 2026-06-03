using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.ProductVariants.Commands;

// ----- Add variant -----
public sealed record AddProductVariantCommand(
    Guid ProductId, string Sku, string? Name, string? Color, string? Size,
    decimal Price, int StockQuantity
) : IRequest<Result<Guid>>;

public sealed class AddProductVariantCommandValidator : AbstractValidator<AddProductVariantCommand>
{
    public AddProductVariantCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0);
    }
}

public sealed class AddProductVariantCommandHandler : IRequestHandler<AddProductVariantCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    public AddProductVariantCommandHandler(IApplicationDbContext db, ICurrentUserService user) { _db = db; _user = user; }

    public async Task<Result<Guid>> Handle(AddProductVariantCommand req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();
        var product = await _db.Products
            .Include(p => p.VendorStore).ThenInclude(s => s.Vendor)
            .FirstOrDefaultAsync(p => p.Id == req.ProductId, ct);
        if (product is null) return Result<Guid>.Failure("Product not found.");
        if (product.VendorStore.Vendor.OwnerUserId != userId) throw new ForbiddenAccessException();

        var sku = req.Sku.Trim();
        if (await _db.ProductVariants.AnyAsync(v => v.Sku == sku, ct))
            return Result<Guid>.Failure($"SKU '{sku}' is already in use.");

        var v = new ProductVariant
        {
            ProductId = product.Id,
            Sku = sku,
            Name = req.Name?.Trim(),
            Color = req.Color?.Trim(),
            Size = req.Size?.Trim(),
            Price = req.Price,
            StockQuantity = req.StockQuantity,
            IsActive = true
        };
        _db.ProductVariants.Add(v);
        await _db.SaveChangesAsync(ct);
        return Result<Guid>.Success(v.Id);
    }
}

// ----- Update variant -----
public sealed record UpdateProductVariantCommand(
    Guid VariantId, string? Name, string? Color, string? Size,
    decimal Price, int StockQuantity, bool IsActive
) : IRequest<Result>;

public sealed class UpdateProductVariantCommandValidator : AbstractValidator<UpdateProductVariantCommand>
{
    public UpdateProductVariantCommandValidator()
    {
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateProductVariantCommandHandler : IRequestHandler<UpdateProductVariantCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    public UpdateProductVariantCommandHandler(IApplicationDbContext db, ICurrentUserService user) { _db = db; _user = user; }

    public async Task<Result> Handle(UpdateProductVariantCommand req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();
        var v = await _db.ProductVariants
            .Include(x => x.Product).ThenInclude(p => p.VendorStore).ThenInclude(s => s.Vendor)
            .FirstOrDefaultAsync(x => x.Id == req.VariantId, ct);
        if (v is null) return Result.Failure("Variant not found.");
        if (v.Product.VendorStore.Vendor.OwnerUserId != userId) throw new ForbiddenAccessException();

        v.Name = req.Name?.Trim();
        v.Color = req.Color?.Trim();
        v.Size = req.Size?.Trim();
        v.Price = req.Price;
        v.StockQuantity = req.StockQuantity;
        v.IsActive = req.IsActive;

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ----- Delete variant -----
public sealed record DeleteProductVariantCommand(Guid VariantId) : IRequest<Result>;

public sealed class DeleteProductVariantCommandHandler : IRequestHandler<DeleteProductVariantCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    public DeleteProductVariantCommandHandler(IApplicationDbContext db, ICurrentUserService user) { _db = db; _user = user; }

    public async Task<Result> Handle(DeleteProductVariantCommand req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();
        var v = await _db.ProductVariants
            .Include(x => x.Product).ThenInclude(p => p.VendorStore).ThenInclude(s => s.Vendor)
            .FirstOrDefaultAsync(x => x.Id == req.VariantId, ct);
        if (v is null) return Result.Failure("Variant not found.");
        if (v.Product.VendorStore.Vendor.OwnerUserId != userId) throw new ForbiddenAccessException();

        var hasOrders = await _db.OrderItems.AnyAsync(o => o.ProductVariantId == v.Id, ct);
        if (hasOrders) return Result.Failure("This variant has been ordered before — deactivate it instead of deleting.");

        var hasCart = await _db.CartItems.AnyAsync(c => c.ProductVariantId == v.Id, ct);
        if (hasCart) return Result.Failure("This variant is currently in a customer's cart.");

        _db.ProductVariants.Remove(v);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
