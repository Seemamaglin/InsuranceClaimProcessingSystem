using InsuranceClaimSystem.Application.DTOs.Notifications;

namespace InsuranceClaimSystem.Application.Interfaces.Services;

public interface INotificationDispatcher
{
    Task SendToUserAsync(Guid userId, NotificationDto notification);
}