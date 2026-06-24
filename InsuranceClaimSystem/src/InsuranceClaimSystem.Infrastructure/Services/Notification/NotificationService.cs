using Hangfire;
using Microsoft.Extensions.Logging;
using InsuranceClaimSystem.Application.Common;
using InsuranceClaimSystem.Application.DTOs.Notifications;
using InsuranceClaimSystem.Application.Interfaces.External;
using InsuranceClaimSystem.Application.Interfaces.Repositories;
using InsuranceClaimSystem.Application.Interfaces.Services;
using InsuranceClaimSystem.Domain.Entities;
using InsuranceClaimSystem.Domain.Enums;

namespace InsuranceClaimSystem.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationRepository notificationRepository,
        IUserRepository userRepository,
        IEmailService emailService,
        INotificationDispatcher notificationDispatcher,
        IBackgroundJobClient backgroundJobClient,
        IUnitOfWork unitOfWork,
        ILogger<NotificationService> logger)
    {
        _notificationRepository = notificationRepository;
        _userRepository = userRepository;
        _emailService = emailService;
        _notificationDispatcher = notificationDispatcher;
        _backgroundJobClient = backgroundJobClient;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<bool>> CreateNotificationAsync(
        Guid recipientId,
        string title,
        string message,
        NotificationType type,
        NotificationChannel channel,
        Guid? claimId = null)
    {
        _logger.LogInformation(
            "Creating notification for recipient {RecipientId} of type {Type} via {Channel}",
            recipientId, type, channel);
        try
        {
            var recipient = await _userRepository.GetByIdAsync(recipientId);
            if (recipient == null)
            {
                _logger.LogWarning("Recipient {RecipientId} not found", recipientId);
                return Result<bool>.Failure(
                    Error.NotFound("RecipientNotFound", "Recipient user not found."));
            }

            var notification = BuildNotificationEntity(recipientId, claimId, title, message, type, channel);

            await _notificationRepository.AddAsync(notification);
            await _unitOfWork.SaveChangesAsync();

            await SendViaChannelsAsync(recipient.Email, channel, notification);

            _logger.LogInformation(
                "Notification created successfully for user {RecipientId} of type {Type} via {Channel}",
                recipientId, type, channel);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating notification for recipient {RecipientId}", recipientId);
            return Result<bool>.Failure(
                Error.Validation("CreateNotificationFailed", "An error occurred while creating the notification."));
        }
    }

    public async Task<Result<PagedResult<NotificationDto>>> GetNotificationsAsync(
        Guid recipientId, 
        int page, 
        int pageSize)
    {
        try
        {
            var notifications = await _notificationRepository.GetByRecipientAsync(recipientId);
            var totalCount = notifications.Count();

            var pagedNotifications = notifications
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    ClaimId = n.ClaimId,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type,
                    Channel = n.Channel,
                    SentAt = n.SentAt,
                    IsRead = n.IsRead,
                    ReadAt = n.ReadAt
                })
                .ToList();

            return Result<PagedResult<NotificationDto>>.Success(
                PagedResult<NotificationDto>.Create(pagedNotifications, totalCount, page, pageSize));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notifications for recipient {RecipientId}", recipientId);
            return Result<PagedResult<NotificationDto>>.Failure(
                Error.Validation("GetNotificationsFailed", "An error occurred while retrieving notifications."));
        }
    }

    public async Task<Result<bool>> MarkAsReadAsync(Guid notificationId, Guid recipientId)
    {
        try
        {
            var notification = await _notificationRepository.GetByIdAsync(notificationId);
            if (notification == null)
            {
                return Result<bool>.Failure(
                    Error.NotFound("NotificationNotFound", "Notification not found."));
            }

            if (notification.RecipientUserId != recipientId)
            {
                return Result<bool>.Failure(
                    Error.Unauthorized("Unauthorized", "You are not authorized to modify this notification."));
            }

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;

            await _notificationRepository.UpdateAsync(notification);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Notification {NotificationId} marked as read by user {RecipientId}",
                notificationId, recipientId);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as read", notificationId);
            return Result<bool>.Failure(
                Error.Validation("MarkAsReadFailed", "An error occurred while marking the notification as read."));
        }
    }

    public async Task<Result<bool>> MarkAllAsReadAsync(Guid recipientId)
    {
        try
        {
            await _notificationRepository.MarkAllAsReadAsync(recipientId);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("All notifications marked as read for user {RecipientId}", recipientId);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read for recipient {RecipientId}", recipientId);
            return Result<bool>.Failure(
                Error.Validation("MarkAllAsReadFailed", "An error occurred while marking notifications as read."));
        }
    }

    private static Notification BuildNotificationEntity(
        Guid recipientId,
        Guid? claimId,
        string title,
        string message,
        NotificationType type,
        NotificationChannel channel)
    {
        return new Notification
        {
            RecipientUserId = recipientId,
            ClaimId = claimId,
            Title = title,
            Message = message,
            Type = type,
            Channel = channel,
            IsRead = false,
            SentAt = DateTime.UtcNow
        };
    }

    private async Task SendViaChannelsAsync(
        string recipientEmail, 
        NotificationChannel channel, 
        Notification notification)
    {
        // Always send email for notifications as requested by user, dispatched via Hangfire
        _backgroundJobClient.Enqueue<IEmailService>(emailService => 
            emailService.SendEmailAsync(recipientEmail, notification.Title, notification.Message, true));

        if (channel == NotificationChannel.InApp)
        {
            await SendInAppNotificationAsync(notification);
        }
    }

    private async Task SendEmailNotificationAsync(string email, string title, string body)
    {
        try
        {
            await _emailService.SendEmailAsync(email, title, body, isHtml: true);
            _logger.LogInformation("Email notification sent to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send email notification to {Email}", email);
        }
    }

    private async Task SendInAppNotificationAsync(Notification notification)
    {
        try
        {
            var dto = new NotificationDto
            {
                Id = notification.Id,
                ClaimId = notification.ClaimId,
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type,
                Channel = notification.Channel,
                SentAt = notification.SentAt,
                IsRead = notification.IsRead,
                ReadAt = notification.ReadAt
            };

            await _notificationDispatcher.SendToUserAsync(notification.RecipientUserId, dto);

            _logger.LogInformation(
                "In-app notification {NotificationId} sent to user {UserId}",
                notification.Id, notification.RecipientUserId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send in-app notification {NotificationId}", notification.Id);
        }
    }
}