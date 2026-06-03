using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Enums;
using MediatR;

namespace Application.Refunds.Queries;

public sealed record RefundRequestDto(
    Guid OrderId,
    string OrderNumber,
    string CustomerUserId,
    string? CustomerName,
    decimal Total,
    DateTime RefundRequestedAtUtc,
    string? RefundReason,
    OrderStatus Status
);

public sealed record GetRefundRequestsQuery(
    bool? PendingOnly = true,
    string? Search = null,
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    int Page = 1,
    int PageSize = 15
) : IRequest<PaginatedList<RefundRequestDto>>;

public sealed class GetRefundRequestsQueryHandler : IRequestHandler<GetRefundRequestsQuery, PaginatedList<RefundRequestDto>>
{
    private readonly IApplicationDbContext _db;
    public GetRefundRequestsQueryHandler(IApplicationDbContext db) => _db = db;

    public Task<PaginatedList<RefundRequestDto>> Handle(GetRefundRequestsQuery req, CancellationToken ct)
    {
        var q = _db.Orders.Where(o => o.RefundRequestedAtUtc.HasValue || o.Status == OrderStatus.Refunded);
        if (req.PendingOnly == true) q = q.Where(o => o.Status != OrderStatus.Refunded);
        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.Trim();
            q = q.Where(o => o.OrderNumber.Contains(s) || (o.ShippingFullName != null && o.ShippingFullName.Contains(s)));
        }
        if (req.DateFrom.HasValue) q = q.Where(o => o.RefundRequestedAtUtc >= req.DateFrom.Value);
        if (req.DateTo.HasValue)   q = q.Where(o => o.RefundRequestedAtUtc <= req.DateTo.Value.AddDays(1));

        var projection = q
            .OrderByDescending(o => o.RefundRequestedAtUtc)
            .Select(o => new RefundRequestDto(
                o.Id, o.OrderNumber, o.CustomerUserId, o.ShippingFullName,
                o.Total, o.RefundRequestedAtUtc!.Value, o.RefundReason, o.Status));

        return PaginatedList<RefundRequestDto>.CreateAsync(projection, req.Page, req.PageSize, ct);
    }
}
