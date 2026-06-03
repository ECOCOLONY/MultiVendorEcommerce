using Domain.Common;

namespace Domain.Entities;

/// <summary>Supports JWT refresh-token flows (rotate/revoke).</summary>
public class RefreshToken : BaseAuditableEntity
{
    public string UserId { get; set; } = string.Empty;

    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? ReplacedByTokenHash { get; set; }
}
