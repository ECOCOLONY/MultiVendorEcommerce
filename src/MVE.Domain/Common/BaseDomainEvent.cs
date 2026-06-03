using MediatR;

namespace Domain.Common;

public abstract class BaseDomainEvent : INotification
{
    public DateTime OccurredAtUtc { get; } = DateTime.UtcNow;
}
