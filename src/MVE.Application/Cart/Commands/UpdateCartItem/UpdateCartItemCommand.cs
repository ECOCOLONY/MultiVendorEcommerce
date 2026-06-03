using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Cart.Commands.UpdateCartItem;

public sealed record UpdateCartItemCommand(Guid CartItemId, int Quantity) : IRequest<Result>;

public sealed class UpdateCartItemCommandValidator : AbstractValidator<UpdateCartItemCommand>
{
    public UpdateCartItemCommandValidator()
    {
        RuleFor(x => x.CartItemId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0).LessThanOrEqualTo(100);
    }
}

public sealed class UpdateCartItemCommandHandler : IRequestHandler<UpdateCartItemCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public UpdateCartItemCommandHandler(IApplicationDbContext db, ICurrentUserService user)
    {
        _db = db; _user = user;
    }

    public async Task<Result> Handle(UpdateCartItemCommand req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();

        var item = await _db.CartItems
            .Include(i => i.Cart)
            .Include(i => i.ProductVariant)
            .FirstOrDefaultAsync(i => i.Id == req.CartItemId, ct);

        if (item is null) return Result.Failure("Item not found.");
        if (item.Cart.CustomerUserId != userId) throw new ForbiddenAccessException();

        if (req.Quantity == 0)
        {
            _db.CartItems.Remove(item);
        }
        else
        {
            if (item.ProductVariant.StockQuantity < req.Quantity)
                return Result.Failure($"Only {item.ProductVariant.StockQuantity} in stock.");
            item.Quantity = req.Quantity;
            item.UnitPrice = item.ProductVariant.Price;
        }

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
