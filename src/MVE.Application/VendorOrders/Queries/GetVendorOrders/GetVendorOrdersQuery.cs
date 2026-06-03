using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.VendorOrders.Queries.GetVendorOrders;

public sealed record VendorOrderListItemDto(
    Guid OrderId,
    string OrderNumber,
    DateTime PlacedAtUtc,
    OrderStatus OrderStatus,
    string CustomerShippingName,
    int MyItemCount,
    int MyTotalQty,
    decimal MyRevenue,
    decimal MyVendorNet,
    int PendingItemCount
);

public sealed record GetVendorOrdersQuery(
    OrderStatus? Status = null,
    string? Search = null,
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    int Page = 1,
    int PageSize = 15
) : IRequest<PaginatedList<VendorOrderListItemDto>>;

public sealed class GetVendorOrdersQueryHandler : IRequestHandler<GetVendorOrdersQuery, PaginatedList<VendorOrderListItemDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public GetVendorOrdersQueryHandler(IApplicationDbContext db, ICurrentUserService user)
    {
        _db = db; _user = user;
    }

    public async Task<PaginatedList<VendorOrderListItemDto>> Handle(GetVendorOrdersQuery req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();

        var storeIds = await _db.VendorStores
            .Where(s => s.Vendor.OwnerUserId == userId)
            .Select(s => s.Id)
            .ToArrayAsync(ct);

        if (storeIds.Length == 0)
            return new PaginatedList<VendorOrderListItemDto>(Array.Empty<VendorOrderListItemDto>(), 0, 1, req.PageSize);

        var q = _db.Orders.Where(o => o.Items.Any(i => storeIds.Contains(i.VendorStoreId)));
        if (req.Status.HasValue) q = q.Where(o => o.Status == req.Status.Value);
        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.Trim();
            q = q.Where(o => o.OrderNumber.Contains(s) || (o.ShippingFullName != null && o.ShippingFullName.Contains(s)));
        }
        if (req.DateFrom.HasValue) q = q.Where(o => (o.PlacedAtUtc ?? o.CreatedAtUtc) >= req.DateFrom.Value);
        if (req.DateTo.HasValue)   q = q.Where(o => (o.PlacedAtUtc ?? o.CreatedAtUtc) <= req.DateTo.Value.AddDays(1));

        var projection = q
            .OrderByDescending(o => o.PlacedAtUtc)
            .Select(o => new VendorOrderListItemDto(
                o.Id,
                o.OrderNumber,
                o.PlacedAtUtc ?? o.CreatedAtUtc,
                o.Status,
                o.ShippingFullName ?? "—",
                o.Items.Count(i => storeIds.Contains(i.VendorStoreId)),
                o.Items.Where(i => storeIds.Contains(i.VendorStoreId)).Sum(i => i.Quantity),
                o.Items.Where(i => storeIds.Contains(i.VendorStoreId)).Sum(i => i.LineTotal),
                o.Items.Where(i => storeIds.Contains(i.VendorStoreId)).Sum(i => i.VendorNetAmount),
                o.Items.Count(i => storeIds.Contains(i.VendorStoreId) && (int)i.VendorFulfillmentStatus < (int)VendorOrderItemStatus.Shipped)));

        return await PaginatedList<VendorOrderListItemDto>.CreateAsync(projection, req.Page, req.PageSize, ct);
    }
}
