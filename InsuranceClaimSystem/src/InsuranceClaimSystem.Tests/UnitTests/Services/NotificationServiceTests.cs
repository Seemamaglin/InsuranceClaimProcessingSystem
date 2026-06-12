using FluentAssertions;
using InsuranceClaimSystem.Application.DTOs.Notifications;
using InsuranceClaimSystem.Application.Interfaces.External;
using InsuranceClaimSystem.Application.Interfaces.Repositories;
using InsuranceClaimSystem.Application.Interfaces.Services;
using InsuranceClaimSystem.Domain.Entities;
using InsuranceClaimSystem.Domain.Enums;
using InsuranceClaimSystem.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InsuranceClaimSystem.Tests.UnitTests.Services;

public class NotificationServiceTests
{
    private readonly Mock<INotificationRepository> _notificationRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<INotificationDispatcher> _notificationDispatcherMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<NotificationService>> _loggerMock;
    private readonly NotificationService _notificationService;

    public NotificationServiceTests()
    {
        _notificationRepositoryMock = new Mock<INotificationRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _emailServiceMock = new Mock<IEmailService>();
        _notificationDispatcherMock = new Mock<INotificationDispatcher>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<NotificationService>>();

        _notificationService = new NotificationService(
            _notificationRepositoryMock.Object,
            _userRepositoryMock.Object,
            _emailServiceMock.Object,
            _notificationDispatcherMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    [Fact]
    public async Task CreateNotificationAsync_WithValidRecipient_ShouldCreateAndSend()
    {
        // Arrange
        var recipientId = Guid.NewGuid();
        var recipient = new User { Id = recipientId, Email = "user@example.com" };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(recipientId)).ReturnsAsync(recipient);
        _notificationRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Notification>())).ReturnsAsync((Notification n) => n);
        _emailServiceMock.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(Task.CompletedTask);
        _notificationDispatcherMock.Setup(x => x.SendToUserAsync(It.IsAny<Guid>(), It.IsAny<NotificationDto>())).Returns(Task.CompletedTask);

        // Act
        var result = await _notificationService.CreateNotificationAsync(
            recipientId,
            "Test Notification",
            "Test message",
            NotificationType.ClaimSubmitted,
            NotificationChannel.Email | NotificationChannel.InApp);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        _notificationRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Notification>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateNotificationAsync_WithNonExistingRecipient_ShouldReturnNotFound()
    {
        // Arrange
        var recipientId = Guid.NewGuid();
        _userRepositoryMock.Setup(x => x.GetByIdAsync(recipientId)).ReturnsAsync((User?)null);

        // Act
        var result = await _notificationService.CreateNotificationAsync(
            recipientId,
            "Test Notification",
            "Test message",
            NotificationType.ClaimSubmitted,
            NotificationChannel.Email);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("RecipientNotFound");
    }

    [Fact]
    public async Task CreateNotificationAsync_WithEmailChannel_ShouldSendEmail()
    {
        // Arrange
        var recipientId = Guid.NewGuid();
        var recipient = new User { Id = recipientId, Email = "user@example.com" };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(recipientId)).ReturnsAsync(recipient);
        _notificationRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Notification>())).ReturnsAsync((Notification n) => n);
        _emailServiceMock.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(Task.CompletedTask);

        // Act
        var result = await _notificationService.CreateNotificationAsync(
            recipientId,
            "Test Notification",
            "Test message",
            NotificationType.ClaimSubmitted,
            NotificationChannel.Email);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _emailServiceMock.Verify(x => x.SendEmailAsync("user@example.com", "Test Notification", "Test message", true), Times.Once);
    }

    [Fact]
    public async Task CreateNotificationAsync_WithInAppChannel_ShouldSendInApp()
    {
        // Arrange
        var recipientId = Guid.NewGuid();
        var recipient = new User { Id = recipientId, Email = "user@example.com" };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(recipientId)).ReturnsAsync(recipient);
        _notificationRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Notification>())).ReturnsAsync((Notification n) => n);
        _notificationDispatcherMock.Setup(x => x.SendToUserAsync(It.IsAny<Guid>(), It.IsAny<NotificationDto>())).Returns(Task.CompletedTask);

        // Act
        var result = await _notificationService.CreateNotificationAsync(
            recipientId,
            "Test Notification",
            "Test message",
            NotificationType.ClaimSubmitted,
            NotificationChannel.InApp);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _notificationDispatcherMock.Verify(x => x.SendToUserAsync(recipientId, It.IsAny<NotificationDto>()), Times.Once);
    }

    [Fact]
    public async Task GetNotificationsAsync_WithValidRecipient_ShouldReturnPaged()
    {
        // Arrange
        var recipientId = Guid.NewGuid();
        var notifications = new List<Notification>
        {
            new() { Id = Guid.NewGuid(), RecipientUserId = recipientId, Title = "Notification 1", Message = "Message 1" },
            new() { Id = Guid.NewGuid(), RecipientUserId = recipientId, Title = "Notification 2", Message = "Message 2" }
        };

        _notificationRepositoryMock.Setup(x => x.GetByRecipientAsync(recipientId, false))
            .ReturnsAsync(notifications.AsEnumerable());

        // Act
        var result = await _notificationService.GetNotificationsAsync(recipientId, 1, 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task MarkAsReadAsync_WithValidNotification_ShouldMarkRead()
    {
        // Arrange
        var notificationId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        var notification = new Notification
        {
            Id = notificationId,
            RecipientUserId = recipientId,
            IsRead = false
        };

        _notificationRepositoryMock.Setup(x => x.GetByIdAsync(notificationId)).ReturnsAsync(notification);
        _notificationRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Notification>())).Returns(Task.CompletedTask);

        // Act
        var result = await _notificationService.MarkAsReadAsync(notificationId, recipientId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        notification.IsRead.Should().BeTrue();
        notification.ReadAt.Should().NotBeNull();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkAsReadAsync_WithNonExistingNotification_ShouldReturnNotFound()
    {
        // Arrange
        var notificationId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        _notificationRepositoryMock.Setup(x => x.GetByIdAsync(notificationId)).ReturnsAsync((Notification?)null);

        // Act
        var result = await _notificationService.MarkAsReadAsync(notificationId, recipientId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("NotificationNotFound");
    }

    [Fact]
    public async Task MarkAsReadAsync_WithWrongRecipient_ShouldReturnUnauthorized()
    {
        // Arrange
        var notificationId = Guid.NewGuid();
        var correctRecipientId = Guid.NewGuid();
        var wrongRecipientId = Guid.NewGuid();
        var notification = new Notification
        {
            Id = notificationId,
            RecipientUserId = correctRecipientId,
            IsRead = false
        };

        _notificationRepositoryMock.Setup(x => x.GetByIdAsync(notificationId)).ReturnsAsync(notification);

        // Act
        var result = await _notificationService.MarkAsReadAsync(notificationId, wrongRecipientId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task MarkAllAsReadAsync_WithValidRecipient_ShouldMarkAllRead()
    {
        // Arrange
        var recipientId = Guid.NewGuid();
        _notificationRepositoryMock.Setup(x => x.MarkAllAsReadAsync(recipientId)).Returns(Task.CompletedTask);

        // Act
        var result = await _notificationService.MarkAllAsReadAsync(recipientId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        _notificationRepositoryMock.Verify(x => x.MarkAllAsReadAsync(recipientId), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateNotificationAsync_WhenEmailFails_ShouldStillCreateNotification()
    {
        // Arrange
        var recipientId = Guid.NewGuid();
        var recipient = new User { Id = recipientId, Email = "user@example.com" };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(recipientId)).ReturnsAsync(recipient);
        _notificationRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Notification>())).ReturnsAsync((Notification n) => n);
        _emailServiceMock.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ThrowsAsync(new Exception("SMTP error"));
        _notificationDispatcherMock.Setup(x => x.SendToUserAsync(It.IsAny<Guid>(), It.IsAny<NotificationDto>())).Returns(Task.CompletedTask);

        // Act
        var result = await _notificationService.CreateNotificationAsync(
            recipientId,
            "Test Notification",
            "Test message",
            NotificationType.ClaimSubmitted,
            NotificationChannel.Email | NotificationChannel.InApp);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        _notificationRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Notification>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}