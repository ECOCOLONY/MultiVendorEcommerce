using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Shipments.Commands.MarkShipmentDelivered;

public sealed record MarkShipmentDeliveredCommand(Guid ShipmentId) : IRequest<Result>;

public sealed class MarkShipmentDeliveredCommandHandler : IRequestHandler<MarkShipmentDeliveredCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    private readonly IDateTimeService _clock;

    public MarkShipmentDeliveredCommandHandler(IApplicationDbContext db, ICurrentUserService user, IDateTimeService clock)
    {
        _db = db; _user = user; _clock = clock;
    }

    public async Task<Result> Handle(MarkShipmentDeliveredCommand req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();

        var shipment = await _db.Shipments
            .Include(s => s.VendorStore).ThenInclude(s => s.Vendor)
            .Include(s => s.Order).ThenInclude(o => o.Items)
            .FirstOrDefaultAsync(s => s.Id == req.ShipmentId, ct);

        if (shipment is null) return Result.Failure("Shipment not found.");
        if (shipment.VendorStore.Vendor.OwnerUserId != userId) throw new ForbiddenAccessException();

        shipment.Status = ShipmentStatus.Delivered;
        shipment.DeliveredAtUtc = _clock.UtcNow;

        // Move all my items in this order to Delivered.
        var myStoreId = shipment.VendorStoreId;
        foreach (var i in shipment.Order.Items.Where(i => i.VendorStoreId == myStoreId))
            i.VendorFulfillmentStatus = VendorOrderItemStatus.Delivered;

        // If every item across all vendors is Delivered, close the order.
        if (shipment.Order.Items.All(i => i.VendorFulfillmentStatus == VendorOrderItemStatus.Delivered))
            shipment.Order.Status = OrderStatus.Delivered;

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
