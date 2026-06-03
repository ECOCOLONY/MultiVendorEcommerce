using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class Notification : BaseAuditableEntity
{
    public string UserId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public NotificationType Type { get; set; }

    /// <summary>Optional in-app link the user is taken to when clicking the notification.</summary>
    public string? ActionUrl { get; set; }

    public bool IsRead { get; set; }
    public DateTime? ReadAtUtc { get; set; }
}
