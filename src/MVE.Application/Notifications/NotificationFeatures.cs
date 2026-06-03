using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Notifications;

public sealed record NotificationDto(
    Guid Id,
    string Title,
    string Body,
    NotificationType Type,
    string? ActionUrl,
    bool IsRead,
    DateTime CreatedAtUtc
);

public sealed record GetMyNotificationsQuery() : IRequest<List<NotificationDto>>;

public sealed class GetMyNotificationsQueryHandler : IRequestHandler<GetMyNotificationsQuery, List<NotificationDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    public GetMyNotificationsQueryHandler(IApplicationDbContext db, ICurrentUserService user) { _db = db; _user = user; }

    public async Task<List<NotificationDto>> Handle(GetMyNotificationsQuery req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();
        return await _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderBy(n => n.IsRead).ThenByDescending(n => n.CreatedAtUtc)
            .Take(50)
            .Select(n => new NotificationDto(n.Id, n.Title, n.Body, n.Type, n.ActionUrl, n.IsRead, n.CreatedAtUtc))
            .ToListAsync(ct);
    }
}

public sealed record GetMyUnreadCountQuery() : IRequest<int>;

public sealed class GetMyUnreadCountQueryHandler : IRequestHandler<GetMyUnreadCountQuery, int>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    public GetMyUnreadCountQueryHandler(IApplicationDbContext db, ICurrentUserService user) { _db = db; _user = user; }

    public async Task<int> Handle(GetMyUnreadCountQuery req, CancellationToken ct)
    {
        var userId = _user.UserId;
        if (string.IsNullOrEmpty(userId)) return 0;
        return await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, ct);
    }
}

public sealed record MarkNotificationReadCommand(Guid Id) : IRequest<Result>;

public sealed class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    private readonly IDateTimeService _clock;
    public MarkNotificationReadCommandHandler(IApplicationDbContext db, ICurrentUserService user, IDateTimeService clock)
    { _db = db; _user = user; _clock = clock; }

    public async Task<Result> Handle(MarkNotificationReadCommand req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();
        var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        if (n is null) return Result.Failure("Notification not found.");
        if (n.UserId != userId) throw new ForbiddenAccessException();
        if (!n.IsRead) { n.IsRead = true; n.ReadAtUtc = _clock.UtcNow; await _db.SaveChangesAsync(ct); }
        return Result.Success();
    }
}

public sealed record MarkAllNotificationsReadCommand() : IRequest<Result>;

public sealed class MarkAllNotificationsReadCommandHandler : IRequestHandler<MarkAllNotificationsReadCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    private readonly IDateTimeService _clock;
    public MarkAllNotificationsReadCommandHandler(IApplicationDbContext db, ICurrentUserService user, IDateTimeService clock)
    { _db = db; _user = user; _clock = clock; }

    public async Task<Result> Handle(MarkAllNotificationsReadCommand req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();
        var unread = await _db.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync(ct);
        var now = _clock.UtcNow;
        foreach (var n in unread) { n.IsRead = true; n.ReadAtUtc = now; }
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
