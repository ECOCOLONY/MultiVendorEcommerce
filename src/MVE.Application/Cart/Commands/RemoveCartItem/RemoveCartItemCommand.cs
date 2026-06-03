using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Cart.Commands.RemoveCartItem;

public sealed record RemoveCartItemCommand(Guid CartItemId) : IRequest<Result>;

public sealed class RemoveCartItemCommandHandler : IRequestHandler<RemoveCartItemCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public RemoveCartItemCommandHandler(IApplicationDbContext db, ICurrentUserService user)
    {
        _db = db; _user = user;
    }

    public async Task<Result> Handle(RemoveCartItemCommand req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();

        var item = await _db.CartItems.Include(i => i.Cart)
            .FirstOrDefaultAsync(i => i.Id == req.CartItemId, ct);

        if (item is null) return Result.Failure("Item not found.");
        if (item.Cart.CustomerUserId != userId) throw new ForbiddenAccessException();

        _db.CartItems.Remove(item);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
