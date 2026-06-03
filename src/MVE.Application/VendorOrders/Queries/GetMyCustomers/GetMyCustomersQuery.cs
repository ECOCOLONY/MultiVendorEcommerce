using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.VendorOrders.Queries.GetMyCustomers;

public sealed record MyCustomerDto(
    string CustomerUserId,
    string ShippingFullName,
    int OrderCount,
    decimal TotalSpentMine,
    DateTime LastOrderAtUtc
);

public sealed record GetMyCustomersQuery(
    string? Search = null,
    int Page = 1,
    int PageSize = 15
) : IRequest<PaginatedList<MyCustomerDto>>;

public sealed class GetMyCustomersQueryHandler : IRequestHandler<GetMyCustomersQuery, PaginatedList<MyCustomerDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public GetMyCustomersQueryHandler(IApplicationDbContext db, ICurrentUserService user)
    { _db = db; _user = user; }

    public async Task<PaginatedList<MyCustomerDto>> Handle(GetMyCustomersQuery req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();

        var storeIds = await _db.VendorStores
            .Where(s => s.Vendor.OwnerUserId == userId)
            .Select(s => s.Id).ToArrayAsync(ct);
        if (storeIds.Length == 0)
            return new PaginatedList<MyCustomerDto>(Array.Empty<MyCustomerDto>(), 0, 1, req.PageSize);

        // Pull a flat row per OrderItem, then group + aggregate in memory.
        var rows = await _db.OrderItems
            .Where(i => storeIds.Contains(i.VendorStoreId))
            .Select(i => new
            {
                i.Order.CustomerUserId,
                ShippingName = i.Order.ShippingFullName,
                i.OrderId,
                i.LineTotal,
                LastDate = i.Order.PlacedAtUtc ?? i.Order.CreatedAtUtc
            })
            .ToListAsync(ct);

        var all = rows
            .GroupBy(r => r.CustomerUserId)
            .Select(g => new MyCustomerDto(
                g.Key,
                g.Select(x => x.ShippingName).FirstOrDefault(s => !string.IsNullOrEmpty(s)) ?? "—",
                g.Select(x => x.OrderId).Distinct().Count(),
                g.Sum(x => x.LineTotal),
                g.Max(x => x.LastDate)))
            .OrderByDescending(c => c.LastOrderAtUtc)
            .AsEnumerable();

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.Trim();
            all = all.Where(c => c.ShippingFullName.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        var list = all.ToList();
        var paged = list.Skip((req.Page - 1) * req.PageSize).Take(req.PageSize).ToList();
        return new PaginatedList<MyCustomerDto>(paged, list.Count, req.Page, req.PageSize);
    }
}
