using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Enums;
using MediatR;

namespace Application.Orders.Queries.GetMyOrders;

public sealed record MyOrderListItemDto(
    Guid Id,
    string OrderNumber,
    DateTime PlacedAtUtc,
    OrderStatus Status,
    decimal Total,
    int ItemCount
);

public sealed record GetMyOrdersQuery(int Page = 1, int PageSize = 10) : IRequest<PaginatedList<MyOrderListItemDto>>;

public sealed class GetMyOrdersQueryHandler : IRequestHandler<GetMyOrdersQuery, PaginatedList<MyOrderListItemDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public GetMyOrdersQueryHandler(IApplicationDbContext db, ICurrentUserService user)
    {
        _db = db; _user = user;
    }

    public Task<PaginatedList<MyOrderListItemDto>> Handle(GetMyOrdersQuery req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();

        var query = _db.Orders
            .Where(o => o.CustomerUserId == userId)
            .OrderByDescending(o => o.PlacedAtUtc)
            .Select(o => new MyOrderListItemDto(
                o.Id, o.OrderNumber,
                o.PlacedAtUtc ?? o.CreatedAtUtc,
                o.Status, o.Total,
                o.Items.Sum(i => i.Quantity)));

        return PaginatedList<MyOrderListItemDto>.CreateAsync(query, req.Page, req.PageSize, ct);
    }
}
