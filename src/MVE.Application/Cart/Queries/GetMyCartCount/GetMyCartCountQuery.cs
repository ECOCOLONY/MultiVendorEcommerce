using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Cart.Queries.GetMyCartCount;

public sealed record GetMyCartCountQuery() : IRequest<int>;

public sealed class GetMyCartCountQueryHandler : IRequestHandler<GetMyCartCountQuery, int>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public GetMyCartCountQueryHandler(IApplicationDbContext db, ICurrentUserService user)
    { _db = db; _user = user; }

    public async Task<int> Handle(GetMyCartCountQuery req, CancellationToken ct)
    {
        var userId = _user.UserId;
        if (string.IsNullOrEmpty(userId)) return 0;

        return await _db.CartItems
            .Where(i => i.Cart.CustomerUserId == userId)
            .SumAsync(i => (int?)i.Quantity, ct) ?? 0;
    }
}
