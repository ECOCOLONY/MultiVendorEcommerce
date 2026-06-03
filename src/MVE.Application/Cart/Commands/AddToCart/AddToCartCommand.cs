using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Cart.Commands.AddToCart;

public sealed record AddToCartCommand(Guid ProductVariantId, int Quantity) : IRequest<Result>;

public sealed class AddToCartCommandValidator : AbstractValidator<AddToCartCommand>
{
    public AddToCartCommandValidator()
    {
        RuleFor(x => x.ProductVariantId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(100);
    }
}

public sealed class AddToCartCommandHandler : IRequestHandler<AddToCartCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public AddToCartCommandHandler(IApplicationDbContext db, ICurrentUserService user)
    {
        _db = db; _user = user;
    }

    public async Task<Result> Handle(AddToCartCommand req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();

        var variant = await _db.ProductVariants
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Id == req.ProductVariantId, ct);

        if (variant is null) return Result.Failure("Variant not found.");
        if (!variant.IsActive) return Result.Failure("This variant is not available.");
        if (!variant.Product.IsPublished || variant.Product.ApprovalStatus != ProductApprovalStatus.Approved)
            return Result.Failure("This product is not currently available.");
        if (variant.StockQuantity < req.Quantity)
            return Result.Failure($"Only {variant.StockQuantity} in stock.");

        var cart = await _db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.CustomerUserId == userId, ct);
        if (cart is null)
        {
            cart = new Domain.Entities.Cart { CustomerUserId = userId };
            _db.Carts.Add(cart);
        }

        var existing = cart.Items.FirstOrDefault(i => i.ProductVariantId == req.ProductVariantId);
        if (existing is not null)
        {
            var newQty = existing.Quantity + req.Quantity;
            if (newQty > variant.StockQuantity)
                return Result.Failure($"Only {variant.StockQuantity} in stock (you already have {existing.Quantity} in your cart).");
            existing.Quantity = newQty;
            existing.UnitPrice = variant.Price;
        }
        else
        {
            cart.Items.Add(new CartItem
            {
                ProductVariantId = variant.Id,
                Quantity = req.Quantity,
                UnitPrice = variant.Price
            });
        }

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
