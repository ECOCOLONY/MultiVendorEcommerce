using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Shipments.Commands.CreateShipment;

public sealed record CreateShipmentCommand(
    Guid OrderId,
    string Carrier,
    string TrackingNumber,
    DateTime? EstimatedDeliveryAtUtc
) : IRequest<Result<Guid>>;

public sealed class CreateShipmentCommandValidator : AbstractValidator<CreateShipmentCommand>
{
    public CreateShipmentCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Carrier).NotEmpty().MaximumLength(80);
        RuleFor(x => x.TrackingNumber).NotEmpty().MaximumLength(120);
    }
}

public sealed class CreateShipmentCommandHandler : IRequestHandler<CreateShipmentCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    private readonly IDateTimeService _clock;

    public CreateShipmentCommandHandler(IApplicationDbContext db, ICurrentUserService user, IDateTimeService clock)
    {
        _db = db; _user = user; _clock = clock;
    }

    public async Task<Result<Guid>> Handle(CreateShipmentCommand req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();

        var storeIds = await _db.VendorStores
            .Where(s => s.Vendor.OwnerUserId == userId)
            .Select(s => s.Id).ToArrayAsync(ct);

        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == req.OrderId, ct);
        if (order is null) return Result<Guid>.Failure("Order not found.");

        var myItems = order.Items.Where(i => storeIds.Contains(i.VendorStoreId)).ToList();
        if (!myItems.Any()) throw new ForbiddenAccessException();

        var myStoreId = myItems.First().VendorStoreId;
        var existing = await _db.Shipments.AnyAsync(s => s.OrderId == order.Id && s.VendorStoreId == myStoreId, ct);
        if (existing) return Result<Guid>.Failure("A shipment already exists for your items in this order.");

        var shipment = new Shipment
        {
            OrderId = order.Id,
            VendorStoreId = myStoreId,
            Carrier = req.Carrier.Trim(),
            TrackingNumber = req.TrackingNumber.Trim(),
            EstimatedDeliveryAtUtc = req.EstimatedDeliveryAtUtc,
            Status = ShipmentStatus.InTransit,
            ShippedAtUtc = _clock.UtcNow
        };
        _db.Shipments.Add(shipment);

        // Items move to Shipped
        foreach (var i in myItems) i.VendorFulfillmentStatus = VendorOrderItemStatus.Shipped;

        if (order.Status == OrderStatus.Paid) order.Status = OrderStatus.Shipped;

        await _db.SaveChangesAsync(ct);
        return Result<Guid>.Success(shipment.Id);
    }
}
