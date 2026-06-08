using Microsoft.EntityFrameworkCore;
using InsuranceClaimSystem.Domain.Entities;
using InsuranceClaimSystem.Infrastructure.Data;
using InsuranceClaimSystem.Application.Interfaces.Repositories;

namespace InsuranceClaimSystem.Infrastructure.Repositories;

public class NotificationRepository : Repository<Notification>, INotificationRepository
{
    public NotificationRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IEnumerable<Notification>> GetByRecipientAsync(Guid recipientId, bool unreadOnly = false)
    {
        var query = _dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientUserId == recipientId);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query
            .OrderByDescending(n => n.SentAt)
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(Guid recipientId)
    {
        return await _dbContext.Notifications
            .CountAsync(n => n.RecipientUserId == recipientId && !n.IsRead);
    }

    public async Task MarkAllAsReadAsync(Guid recipientId)
    {
        var unreadNotifications = await _dbContext.Notifications
            .Where(n => n.RecipientUserId == recipientId && !n.IsRead)
            .ToListAsync();

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
        }
    }
}