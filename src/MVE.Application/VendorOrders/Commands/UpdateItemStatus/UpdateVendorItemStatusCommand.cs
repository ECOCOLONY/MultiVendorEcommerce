using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.VendorOrders.Commands.UpdateItemStatus;

public sealed record UpdateVendorItemStatusCommand(Guid OrderItemId, VendorOrderItemStatus Status) : IRequest<Result>;

public sealed class UpdateVendorItemStatusCommandHandler : IRequestHandler<UpdateVendorItemStatusCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public UpdateVendorItemStatusCommandHandler(IApplicationDbContext db, ICurrentUserService user)
    {
        _db = db; _user = user;
    }

    public async Task<Result> Handle(UpdateVendorItemStatusCommand req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();

        var item = await _db.OrderItems
            .Include(i => i.VendorStore).ThenInclude(s => s.Vendor)
            .Include(i => i.Order).ThenInclude(o => o.Items)
            .FirstOrDefaultAsync(i => i.Id == req.OrderItemId, ct);

        if (item is null) return Result.Failure("Item not found.");
        if (item.VendorStore.Vendor.OwnerUserId != userId) throw new ForbiddenAccessException();

        item.VendorFulfillmentStatus = req.Status;

        // If all items across all vendors are Delivered → close the order.
        if (item.Order.Items.All(i => i.VendorFulfillmentStatus == VendorOrderItemStatus.Delivered))
            item.Order.Status = OrderStatus.Delivered;
        else if (item.Order.Items.Any(i => i.VendorFulfillmentStatus == VendorOrderItemStatus.Shipped)
                 && item.Order.Status == OrderStatus.Paid)
            item.Order.Status = OrderStatus.Shipped;

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
