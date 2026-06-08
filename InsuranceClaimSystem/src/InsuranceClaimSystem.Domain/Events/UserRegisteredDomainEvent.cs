using InsuranceClaimSystem.Domain.Common;
using InsuranceClaimSystem.Domain.Enums;

namespace InsuranceClaimSystem.Domain.Events;

public class UserRegisteredDomainEvent : DomainEvent
{
    public Guid UserId { get; }
    public string Email { get; }
    public string Username { get; }
    public UserRole Role { get; }

    public UserRegisteredDomainEvent(Guid userId, string email, string username, UserRole role)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));

        UserId = userId;
        Email = email ?? throw new ArgumentNullException(nameof(email));
        Username = username ?? throw new ArgumentNullException(nameof(username));
        Role = role;
        EventType = "User.Registered";
    }
}