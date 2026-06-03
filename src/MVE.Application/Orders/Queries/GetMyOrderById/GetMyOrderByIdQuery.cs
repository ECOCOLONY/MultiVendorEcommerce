using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Orders.Queries.GetMyOrderById;

public sealed record MyOrderItemDto(
    Guid Id,
    string ProductName,
    string? VariantName,
    string StoreName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    VendorOrderItemStatus VendorFulfillmentStatus
);

public sealed record MyOrderDetailDto(
    Guid Id,
    string OrderNumber,
    OrderStatus Status,
    DateTime? PlacedAtUtc,
    DateTime? PaidAtUtc,
    DateTime? CancelledAtUtc,
    decimal Subtotal,
    decimal ShippingAmount,
    decimal TaxAmount,
    decimal DiscountAmount,
    decimal Total,
    string? ShippingFullName,
    string? ShippingLine1,
    string? ShippingLine2,
    string? ShippingCity,
    string? ShippingState,
    string? ShippingPostalCode,
    string? ShippingCountry,
    string? ShippingPhone,
    string? PaymentMethod,
    PaymentStatus? PaymentStatus,
    DateTime? RefundRequestedAtUtc,
    string? RefundReason,
    DateTime? RefundedAtUtc,
    List<MyOrderItemDto> Items
);

public sealed record GetMyOrderByIdQuery(Guid Id) : IRequest<MyOrderDetailDto>;

public sealed class GetMyOrderByIdQueryHandler : IRequestHandler<GetMyOrderByIdQuery, MyOrderDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public GetMyOrderByIdQueryHandler(IApplicationDbContext db, ICurrentUserService user)
    {
        _db = db; _user = user;
    }

    public async Task<MyOrderDetailDto> Handle(GetMyOrderByIdQuery req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();

        var o = await _db.Orders
            .Where(x => x.Id == req.Id)
            .Select(x => new
            {
                x.Id, x.OrderNumber, x.Status, x.PlacedAtUtc, x.PaidAtUtc, x.CancelledAtUtc,
                x.Subtotal, x.ShippingAmount, x.TaxAmount, x.DiscountAmount, x.Total,
                x.ShippingFullName, x.ShippingLine1, x.ShippingLine2, x.ShippingCity,
                x.ShippingState, x.ShippingPostalCode, x.ShippingCountry, x.ShippingPhone,
                x.RefundRequestedAtUtc, x.RefundReason, x.RefundedAtUtc,
                x.CustomerUserId,
                Payment = x.Payments.OrderByDescending(p => p.CreatedAtUtc).FirstOrDefault(),
                Items = x.Items.Select(i => new MyOrderItemDto(
                    i.Id, i.ProductName, i.VariantName, i.VendorStore.Name,
                    i.Quantity, i.UnitPrice, i.LineTotal, i.VendorFulfillmentStatus)).ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (o is null) throw new NotFoundException(nameof(Domain.Entities.Order), req.Id);
        if (o.CustomerUserId != userId) throw new ForbiddenAccessException();

        return new MyOrderDetailDto(
            o.Id, o.OrderNumber, o.Status, o.PlacedAtUtc, o.PaidAtUtc, o.CancelledAtUtc,
            o.Subtotal, o.ShippingAmount, o.TaxAmount, o.DiscountAmount, o.Total,
            o.ShippingFullName, o.ShippingLine1, o.ShippingLine2, o.ShippingCity,
            o.ShippingState, o.ShippingPostalCode, o.ShippingCountry, o.ShippingPhone,
            o.Payment?.Provider, o.Payment?.Status,
            o.RefundRequestedAtUtc, o.RefundReason, o.RefundedAtUtc,
            o.Items);
    }
}
