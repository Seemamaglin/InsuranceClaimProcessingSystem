using InsuranceClaimSystem.Domain.Common;
using InsuranceClaimSystem.Domain.Enums;

namespace InsuranceClaimSystem.Domain.Events;

public class NotificationSentDomainEvent : DomainEvent
{
    public Guid NotificationId { get; }
    public Guid RecipientUserId { get; }
    public Guid? ClaimId { get; }
    public NotificationType Type { get; }
    public NotificationChannel Channel { get; }

    public NotificationSentDomainEvent(Guid notificationId, Guid recipientUserId, Guid? claimId, NotificationType type, NotificationChannel channel)
    {
        if (notificationId == Guid.Empty)
            throw new ArgumentException("NotificationId cannot be empty.", nameof(notificationId));
        if (recipientUserId == Guid.Empty)
            throw new ArgumentException("RecipientUserId cannot be empty.", nameof(recipientUserId));

        NotificationId = notificationId;
        RecipientUserId = recipientUserId;
        ClaimId = claimId;
        Type = type;
        Channel = channel;
        EventType = "Notification.Sent";
    }
}