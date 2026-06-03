using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.VendorOrders.Queries.GetVendorOrderById;

public sealed record VendorOrderItemDto(
    Guid Id,
    string ProductName,
    string? VariantName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    decimal CommissionAmount,
    decimal VendorNetAmount,
    VendorOrderItemStatus VendorFulfillmentStatus
);

public sealed record VendorShipmentDto(
    Guid Id,
    string? Carrier,
    string? TrackingNumber,
    ShipmentStatus Status,
    DateTime? ShippedAtUtc,
    DateTime? DeliveredAtUtc
);

public sealed record VendorOrderDetailDto(
    Guid OrderId,
    string OrderNumber,
    DateTime? PlacedAtUtc,
    OrderStatus OrderStatus,
    string? ShippingFullName,
    string? ShippingPhone,
    string? ShippingLine1,
    string? ShippingLine2,
    string? ShippingCity,
    string? ShippingState,
    string? ShippingPostalCode,
    string? ShippingCountry,
    decimal MyRevenue,
    decimal MyVendorNet,
    List<VendorOrderItemDto> Items,
    List<VendorShipmentDto> Shipments
);

public sealed record GetVendorOrderByIdQuery(Guid OrderId) : IRequest<VendorOrderDetailDto>;

public sealed class GetVendorOrderByIdQueryHandler : IRequestHandler<GetVendorOrderByIdQuery, VendorOrderDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public GetVendorOrderByIdQueryHandler(IApplicationDbContext db, ICurrentUserService user)
    {
        _db = db; _user = user;
    }

    public async Task<VendorOrderDetailDto> Handle(GetVendorOrderByIdQuery req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();
        var storeIds = await _db.VendorStores
            .Where(s => s.Vendor.OwnerUserId == userId)
            .Select(s => s.Id).ToArrayAsync(ct);

        var order = await _db.Orders
            .Where(o => o.Id == req.OrderId && o.Items.Any(i => storeIds.Contains(i.VendorStoreId)))
            .Select(o => new
            {
                o.Id, o.OrderNumber, o.PlacedAtUtc, OrderStatus = o.Status,
                o.ShippingFullName, o.ShippingPhone, o.ShippingLine1, o.ShippingLine2,
                o.ShippingCity, o.ShippingState, o.ShippingPostalCode, o.ShippingCountry,
                Items = o.Items
                    .Where(i => storeIds.Contains(i.VendorStoreId))
                    .Select(i => new VendorOrderItemDto(
                        i.Id, i.ProductName, i.VariantName,
                        i.Quantity, i.UnitPrice, i.LineTotal,
                        i.CommissionAmount, i.VendorNetAmount,
                        i.VendorFulfillmentStatus)).ToList(),
                Shipments = o.Shipments
                    .Where(s => storeIds.Contains(s.VendorStoreId))
                    .Select(s => new VendorShipmentDto(s.Id, s.Carrier, s.TrackingNumber, s.Status, s.ShippedAtUtc, s.DeliveredAtUtc)).ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (order is null) throw new NotFoundException("Order", req.OrderId);

        return new VendorOrderDetailDto(
            order.Id, order.OrderNumber, order.PlacedAtUtc, order.OrderStatus,
            order.ShippingFullName, order.ShippingPhone, order.ShippingLine1, order.ShippingLine2,
            order.ShippingCity, order.ShippingState, order.ShippingPostalCode, order.ShippingCountry,
            order.Items.Sum(i => i.LineTotal),
            order.Items.Sum(i => i.VendorNetAmount),
            order.Items, order.Shipments);
    }
}
