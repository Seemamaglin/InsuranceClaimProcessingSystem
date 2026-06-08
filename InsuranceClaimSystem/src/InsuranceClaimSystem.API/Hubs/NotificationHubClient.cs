using InsuranceClaimSystem.Application.DTOs.Notifications;

namespace InsuranceClaimSystem.API.Hubs;

public interface INotificationHubClient
{
    Task ReceiveNotification(NotificationDto notification);
}