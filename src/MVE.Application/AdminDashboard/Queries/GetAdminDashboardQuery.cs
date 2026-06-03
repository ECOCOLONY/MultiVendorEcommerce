using Application.Common.Interfaces;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.AdminDashboard.Queries;

public sealed record AdminDashboardDto(
    int VendorCount,
    int PendingVendorCount,
    int ProductCount,
    int PendingProductCount,
    int OrderCount,
    int CustomerCount,
    decimal TotalRevenue,
    List<DailyPointDto> DailyRevenue
);

public sealed record DailyPointDto(string Date, int Orders, decimal Revenue);

public sealed record GetAdminDashboardQuery(int Days = 14) : IRequest<AdminDashboardDto>;

public sealed class GetAdminDashboardQueryHandler : IRequestHandler<GetAdminDashboardQuery, AdminDashboardDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeService _clock;

    public GetAdminDashboardQueryHandler(IApplicationDbContext db, IDateTimeService clock)
    { _db = db; _clock = clock; }

    public async Task<AdminDashboardDto> Handle(GetAdminDashboardQuery req, CancellationToken ct)
    {
        var vendorCount        = await _db.Vendors.CountAsync(ct);
        var pendingVendorCount = await _db.Vendors.CountAsync(v => !v.IsApproved, ct);
        var productCount       = await _db.Products.CountAsync(ct);
        var pendingProductCount= await _db.Products.CountAsync(p => p.ApprovalStatus == ProductApprovalStatus.Pending, ct);
        var orderCount         = await _db.Orders.CountAsync(ct);
        var totalRevenue       = await _db.Orders.Where(o => o.Status != OrderStatus.Cancelled).SumAsync(o => (decimal?)o.Total, ct) ?? 0m;
        var customerCount      = await _db.Orders.Select(o => o.CustomerUserId).Distinct().CountAsync(ct);

        var since = _clock.UtcNow.AddDays(-req.Days).Date;
        var rows = await _db.Orders
            .Where(o => o.Status != OrderStatus.Cancelled && (o.PlacedAtUtc ?? o.CreatedAtUtc) >= since)
            .Select(o => new { Date = (o.PlacedAtUtc ?? o.CreatedAtUtc).Date, o.Total })
            .ToListAsync(ct);

        var daily = Enumerable.Range(0, req.Days)
            .Select(d => since.AddDays(d))
            .Select(date => new DailyPointDto(
                date.ToString("yyyy-MM-dd"),
                rows.Count(r => r.Date == date),
                rows.Where(r => r.Date == date).Sum(r => r.Total)))
            .ToList();

        return new AdminDashboardDto(
            vendorCount, pendingVendorCount,
            productCount, pendingProductCount,
            orderCount, customerCount, totalRevenue, daily);
    }
}
