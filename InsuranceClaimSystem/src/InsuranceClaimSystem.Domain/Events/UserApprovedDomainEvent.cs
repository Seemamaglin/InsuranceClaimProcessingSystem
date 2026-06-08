using InsuranceClaimSystem.Domain.Common;
using InsuranceClaimSystem.Domain.Enums;

namespace InsuranceClaimSystem.Domain.Events;

public class UserApprovedDomainEvent : DomainEvent
{
    public Guid UserId { get; }
    public string Email { get; }
    public UserRole Role { get; }

    public UserApprovedDomainEvent(Guid userId, string email, UserRole role)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));

        UserId = userId;
        Email = email ?? throw new ArgumentNullException(nameof(email));
        Role = role;
        EventType = "User.Approved";
    }
}