using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;

namespace Infrastructure.Services;

public class AuditLogger : IAuditLogger
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    private readonly IDateTimeService _clock;
    private readonly IHttpContextAccessor _http;

    public AuditLogger(ApplicationDbContext db, ICurrentUserService user, IDateTimeService clock, IHttpContextAccessor http)
    {
        _db = db; _user = user; _clock = clock; _http = http;
    }

    public async Task LogAsync(AuditAction action, string entityName, string? entityId = null,
                               string? oldValuesJson = null, string? newValuesJson = null,
                               CancellationToken ct = default)
    {
        var ip = _http.HttpContext?.Connection.RemoteIpAddress?.ToString();

        _db.AuditLogs.Add(new AuditLog
        {
            UserId = _user.UserId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            OldValues = oldValuesJson,
            NewValues = newValuesJson,
            IpAddress = ip
        });

        // Save without touching other tracked entities.
        await _db.SaveChangesAsync(ct);
    }
}
