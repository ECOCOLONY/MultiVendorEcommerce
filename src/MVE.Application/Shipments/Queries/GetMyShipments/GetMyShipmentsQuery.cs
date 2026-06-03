using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Enums;
using MediatR;

namespace Application.Shipments.Queries.GetMyShipments;

public sealed record VendorShipmentListItemDto(
    Guid Id,
    Guid OrderId,
    string OrderNumber,
    string? Carrier,
    string? TrackingNumber,
    ShipmentStatus Status,
    DateTime CreatedAtUtc,
    DateTime? ShippedAtUtc,
    DateTime? DeliveredAtUtc
);

public sealed record GetMyShipmentsQuery(
    ShipmentStatus? Status = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 15
) : IRequest<PaginatedList<VendorShipmentListItemDto>>;

public sealed class GetMyShipmentsQueryHandler : IRequestHandler<GetMyShipmentsQuery, PaginatedList<VendorShipmentListItemDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public GetMyShipmentsQueryHandler(IApplicationDbContext db, ICurrentUserService user)
    {
        _db = db; _user = user;
    }

    public Task<PaginatedList<VendorShipmentListItemDto>> Handle(GetMyShipmentsQuery req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();

        var q = _db.Shipments.Where(s => s.VendorStore.Vendor.OwnerUserId == userId);
        if (req.Status.HasValue) q = q.Where(s => s.Status == req.Status.Value);
        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.Trim();
            q = q.Where(sh => sh.Order.OrderNumber.Contains(s) || (sh.TrackingNumber != null && sh.TrackingNumber.Contains(s)));
        }

        var projection = q
            .OrderByDescending(s => s.CreatedAtUtc)
            .Select(s => new VendorShipmentListItemDto(
                s.Id, s.OrderId, s.Order.OrderNumber,
                s.Carrier, s.TrackingNumber, s.Status,
                s.CreatedAtUtc, s.ShippedAtUtc, s.DeliveredAtUtc));

        return PaginatedList<VendorShipmentListItemDto>.CreateAsync(projection, req.Page, req.PageSize, ct);
    }
}
