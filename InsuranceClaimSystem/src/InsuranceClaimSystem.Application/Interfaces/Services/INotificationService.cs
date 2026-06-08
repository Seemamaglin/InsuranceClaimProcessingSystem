using InsuranceClaimSystem.Application.Common;
using InsuranceClaimSystem.Application.DTOs.Notifications;
using InsuranceClaimSystem.Domain.Enums;

namespace InsuranceClaimSystem.Application.Interfaces.Services;

public interface INotificationService
{
    Task<Result<bool>> CreateNotificationAsync(
        Guid recipientId,
        string title,
        string message,
        NotificationType type,
        NotificationChannel channel,
        Guid? claimId = null);

    Task<Result<PagedResult<NotificationDto>>> GetNotificationsAsync(Guid recipientId, int page, int pageSize);
    Task<Result<bool>> MarkAsReadAsync(Guid notificationId, Guid recipientId);
    Task<Result<bool>> MarkAllAsReadAsync(Guid recipientId);
}