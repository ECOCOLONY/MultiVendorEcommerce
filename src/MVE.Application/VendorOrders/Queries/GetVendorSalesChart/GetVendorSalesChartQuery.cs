using Application.Common.Exceptions;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.VendorOrders.Queries.GetVendorSalesChart;

public sealed record SalesPointDto(string Date, decimal Revenue, decimal Net);

public sealed record GetVendorSalesChartQuery(int Days = 14) : IRequest<List<SalesPointDto>>;

public sealed class GetVendorSalesChartQueryHandler : IRequestHandler<GetVendorSalesChartQuery, List<SalesPointDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    private readonly IDateTimeService _clock;

    public GetVendorSalesChartQueryHandler(IApplicationDbContext db, ICurrentUserService user, IDateTimeService clock)
    { _db = db; _user = user; _clock = clock; }

    public async Task<List<SalesPointDto>> Handle(GetVendorSalesChartQuery req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();

        var storeIds = await _db.VendorStores
            .Where(s => s.Vendor.OwnerUserId == userId)
            .Select(s => s.Id).ToArrayAsync(ct);
        if (storeIds.Length == 0) return new List<SalesPointDto>();

        var since = _clock.UtcNow.AddDays(-req.Days).Date;

        // Pull flat rows then bucket in memory.
        var rows = await _db.OrderItems
            .Where(i => storeIds.Contains(i.VendorStoreId)
                        && (i.Order.PlacedAtUtc ?? i.Order.CreatedAtUtc) >= since)
            .Select(i => new
            {
                Date = (i.Order.PlacedAtUtc ?? i.Order.CreatedAtUtc).Date,
                i.LineTotal,
                i.VendorNetAmount
            })
            .ToListAsync(ct);

        var buckets = Enumerable.Range(0, req.Days)
            .Select(d => since.AddDays(d))
            .Select(date => new SalesPointDto(
                date.ToString("yyyy-MM-dd"),
                rows.Where(r => r.Date == date).Sum(r => r.LineTotal),
                rows.Where(r => r.Date == date).Sum(r => r.VendorNetAmount)))
            .ToList();

        return buckets;
    }
}
