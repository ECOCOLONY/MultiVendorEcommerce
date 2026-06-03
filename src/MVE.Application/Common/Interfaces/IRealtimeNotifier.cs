namespace Application.Common.Interfaces;

/// <summary>
/// Pushes a real-time notification to a specific user (across any open browser tab).
/// Implementation lives in the Web project (SignalR hub).
/// </summary>
public interface IRealtimeNotifier
{
    Task NotifyUserAsync(string userId, string title, string body, string? actionUrl = null, CancellationToken ct = default);
}
