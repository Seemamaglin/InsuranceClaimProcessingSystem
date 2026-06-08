using InsuranceClaimSystem.Domain.Entities;

namespace InsuranceClaimSystem.Application.Interfaces.Repositories;

public interface INotificationRepository : IRepository<Notification>
{
    Task<IEnumerable<Notification>> GetByRecipientAsync(Guid recipientId, bool unreadOnly = false);
    Task<int> GetUnreadCountAsync(Guid recipientId);
    Task MarkAllAsReadAsync(Guid recipientId);
}