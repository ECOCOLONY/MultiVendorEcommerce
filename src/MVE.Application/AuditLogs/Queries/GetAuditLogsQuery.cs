using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Enums;
using MediatR;

namespace Application.AuditLogs.Queries;

public sealed record AuditLogDto(
    Guid Id,
    DateTime CreatedAtUtc,
    string? UserId,
    AuditAction Action,
    string EntityName,
    string? EntityId,
    string? IpAddress
);

public sealed record GetAuditLogsQuery(
    AuditAction? Action = null,
    string? UserSearch = null,
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<PaginatedList<AuditLogDto>>;

public sealed class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, PaginatedList<AuditLogDto>>
{
    private readonly IApplicationDbContext _db;
    public GetAuditLogsQueryHandler(IApplicationDbContext db) => _db = db;

    public Task<PaginatedList<AuditLogDto>> Handle(GetAuditLogsQuery req, CancellationToken ct)
    {
        var q = _db.AuditLogs.AsQueryable();
        if (req.Action.HasValue) q = q.Where(a => a.Action == req.Action.Value);
        if (!string.IsNullOrWhiteSpace(req.UserSearch))
        {
            var s = req.UserSearch.Trim();
            q = q.Where(a => a.UserId != null && a.UserId.Contains(s));
        }
        if (req.DateFrom.HasValue) q = q.Where(a => a.CreatedAtUtc >= req.DateFrom.Value);
        if (req.DateTo.HasValue)   q = q.Where(a => a.CreatedAtUtc <= req.DateTo.Value.AddDays(1));

        var projection = q
            .OrderByDescending(a => a.CreatedAtUtc)
            .Select(a => new AuditLogDto(a.Id, a.CreatedAtUtc, a.UserId, a.Action, a.EntityName, a.EntityId, a.IpAddress));

        return PaginatedList<AuditLogDto>.CreateAsync(projection, req.Page, req.PageSize, ct);
    }
}
