using Microsoft.AspNetCore.SignalR;
using InsuranceClaimSystem.API.Hubs;
using InsuranceClaimSystem.Application.DTOs.Notifications;
using InsuranceClaimSystem.Application.Interfaces.Services;

namespace InsuranceClaimSystem.API.Services;

public class SignalRNotificationDispatcher : INotificationDispatcher
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<SignalRNotificationDispatcher> _logger;

    public SignalRNotificationDispatcher(
        IHubContext<NotificationHub> hubContext,
        ILogger<SignalRNotificationDispatcher> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendToUserAsync(Guid userId, NotificationDto notification)
    {
        try
        {
            await _hubContext.Clients.User(userId.ToString())
                .SendAsync("ReceiveNotification", notification);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send SignalR notification to user {UserId}", userId);
        }
    }
}