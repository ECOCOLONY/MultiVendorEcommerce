using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class Payment : BaseAuditableEntity
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }

    public string? Provider { get; set; }
    public string? ExternalPaymentId { get; set; }
}
