namespace FlowIQ.Domain.Common;

public abstract class BaseDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
