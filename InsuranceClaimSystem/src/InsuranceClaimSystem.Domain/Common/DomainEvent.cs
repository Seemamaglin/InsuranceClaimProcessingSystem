namespace InsuranceClaimSystem.Domain.Common;

public abstract class DomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public string EventType { get; protected set; } = string.Empty;
}