using Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Web.Realtime;

public class SignalRRealtimeNotifier : IRealtimeNotifier
{
    private readonly IHubContext<NotificationsHub> _hub;
    public SignalRRealtimeNotifier(IHubContext<NotificationsHub> hub) => _hub = hub;

    public Task NotifyUserAsync(string userId, string title, string body, string? actionUrl = null, CancellationToken ct = default) =>
        _hub.Clients.Group($"user:{userId}").SendAsync("notify", new { title, body, actionUrl }, ct);
}
