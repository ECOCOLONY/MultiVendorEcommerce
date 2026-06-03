using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.ProductImages.Commands;

// ----- Add image -----
public sealed record AddProductImageCommand(Guid ProductId, string ImageUrl) : IRequest<Result<Guid>>;

public sealed class AddProductImageCommandValidator : AbstractValidator<AddProductImageCommand>
{
    public AddProductImageCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.ImageUrl).NotEmpty().MaximumLength(500);
    }
}

public sealed class AddProductImageCommandHandler : IRequestHandler<AddProductImageCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    public AddProductImageCommandHandler(IApplicationDbContext db, ICurrentUserService user) { _db = db; _user = user; }

    public async Task<Result<Guid>> Handle(AddProductImageCommand req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();
        var product = await _db.Products
            .Include(p => p.VendorStore).ThenInclude(s => s.Vendor)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == req.ProductId, ct);
        if (product is null) return Result<Guid>.Failure("Product not found.");
        if (product.VendorStore.Vendor.OwnerUserId != userId) throw new ForbiddenAccessException();

        var img = new ProductImage
        {
            ProductId = product.Id,
            Url = req.ImageUrl,
            IsMain = !product.Images.Any(),
            SortOrder = product.Images.Count
        };
        _db.ProductImages.Add(img);
        await _db.SaveChangesAsync(ct);
        return Result<Guid>.Success(img.Id);
    }
}

// ----- Delete image -----
public sealed record DeleteProductImageCommand(Guid ImageId) : IRequest<Result>;

public sealed class DeleteProductImageCommandHandler : IRequestHandler<DeleteProductImageCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    public DeleteProductImageCommandHandler(IApplicationDbContext db, ICurrentUserService user) { _db = db; _user = user; }

    public async Task<Result> Handle(DeleteProductImageCommand req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();
        var image = await _db.ProductImages
            .Include(i => i.Product).ThenInclude(p => p.VendorStore).ThenInclude(s => s.Vendor)
            .Include(i => i.Product).ThenInclude(p => p.Images)
            .FirstOrDefaultAsync(i => i.Id == req.ImageId, ct);
        if (image is null) return Result.Failure("Image not found.");
        if (image.Product.VendorStore.Vendor.OwnerUserId != userId) throw new ForbiddenAccessException();

        var wasMain = image.IsMain;
        _db.ProductImages.Remove(image);

        // Promote a new main if we just removed the main one.
        if (wasMain)
        {
            var next = image.Product.Images.OrderBy(i => i.SortOrder).FirstOrDefault(i => i.Id != image.Id);
            if (next is not null) next.IsMain = true;
        }

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ----- Set main image -----
public sealed record SetMainProductImageCommand(Guid ImageId) : IRequest<Result>;

public sealed class SetMainProductImageCommandHandler : IRequestHandler<SetMainProductImageCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    public SetMainProductImageCommandHandler(IApplicationDbContext db, ICurrentUserService user) { _db = db; _user = user; }

    public async Task<Result> Handle(SetMainProductImageCommand req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();
        var image = await _db.ProductImages
            .Include(i => i.Product).ThenInclude(p => p.VendorStore).ThenInclude(s => s.Vendor)
            .Include(i => i.Product).ThenInclude(p => p.Images)
            .FirstOrDefaultAsync(i => i.Id == req.ImageId, ct);
        if (image is null) return Result.Failure("Image not found.");
        if (image.Product.VendorStore.Vendor.OwnerUserId != userId) throw new ForbiddenAccessException();

        foreach (var i in image.Product.Images) i.IsMain = (i.Id == image.Id);

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
