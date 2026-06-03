using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class AuditLog : BaseAuditableEntity
{
    public string? UserId { get; set; }

    public AuditAction Action { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
}
