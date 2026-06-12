using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InsuranceClaimSystem.Application.Interfaces.Services;

namespace InsuranceClaimSystem.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(INotificationService notificationService, ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated notifications for the current user
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        _logger.LogInformation("API: {Action} called", nameof(GetNotifications));
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            _logger.LogWarning("API: {Action} failed - Unauthorized", nameof(GetNotifications));
            return Unauthorized(new { message = "User not authenticated." });
        }

        var result = await _notificationService.GetNotificationsAsync(userId.Value, page, pageSize);
        if (result.IsFailure)
        {
            _logger.LogWarning("API: {Action} failed - {ErrorCode}", nameof(GetNotifications), result.Error.Code);
            return BadRequest(result.Error);
        }
        _logger.LogInformation("API: {Action} succeeded", nameof(GetNotifications));
        return Ok(result.Value);
    }

    /// <summary>
    /// Mark a notification as read
    /// </summary>
    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        _logger.LogInformation("API: {Action} called", nameof(MarkAsRead));
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            _logger.LogWarning("API: {Action} failed - Unauthorized", nameof(MarkAsRead));
            return Unauthorized(new { message = "User not authenticated." });
        }

        var result = await _notificationService.MarkAsReadAsync(id, userId.Value);
        if (result.IsFailure)
        {
            _logger.LogWarning("API: {Action} failed - {ErrorCode}", nameof(MarkAsRead), result.Error.Code);
            if (result.Error.Code == "NotificationNotFound")
            {
                return NotFound(result.Error);
            }
            if (result.Error.Code == "Unauthorized")
            {
                return Forbid();
            }
            return BadRequest(result.Error);
        }
        _logger.LogInformation("API: {Action} succeeded", nameof(MarkAsRead));
        return Ok(new { success = result.Value });
    }

    /// <summary>
    /// Mark all notifications as read for the current user
    /// </summary>
    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        _logger.LogInformation("API: {Action} called", nameof(MarkAllAsRead));
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            _logger.LogWarning("API: {Action} failed - Unauthorized", nameof(MarkAllAsRead));
            return Unauthorized(new { message = "User not authenticated." });
        }

        var result = await _notificationService.MarkAllAsReadAsync(userId.Value);
        if (result.IsFailure)
        {
            _logger.LogWarning("API: {Action} failed - {ErrorCode}", nameof(MarkAllAsRead), result.Error.Code);
            return BadRequest(result.Error);
        }
        _logger.LogInformation("API: {Action} succeeded", nameof(MarkAllAsRead));
        return Ok(new { success = result.Value });
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return null;
        }

        return userId;
    }
}