namespace InsuranceClaimSystem.Domain.Common
{
    public abstract class BaseEntity
    {
        public Guid Id {get; set; }=Guid.NewGuid();
        public DateTime CreatedAt { get; set; }=DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; }=DateTime.UtcNow;
        public bool IsDeleted { get; set; }=false;
        public DateTime? DeletedAt { get; set; }=null;

        private readonly List<DomainEvent> _domainEvents = new();
        public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        public void AddDomainEvent(DomainEvent eventItem) => _domainEvents.Add(eventItem);
        public void RemoveDomainEvent(DomainEvent eventItem) => _domainEvents.Remove(eventItem);
        public void ClearDomainEvents() => _domainEvents.Clear();
    }
}