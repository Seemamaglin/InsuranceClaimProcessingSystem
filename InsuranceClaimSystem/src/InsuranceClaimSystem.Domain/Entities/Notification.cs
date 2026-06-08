using System;
using InsuranceClaimSystem.Domain.Common;
using InsuranceClaimSystem.Domain.Enums;

namespace InsuranceClaimSystem.Domain.Entities
{
    // Inherits: Id, CreatedAt, UpdatedAt, IsDeleted, DeletedAt from BaseEntity
    public class Notification : BaseEntity
    {
        public Guid RecipientUserId { get; set; }
        public Guid? ClaimId { get; set; }
        
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        
        public NotificationType Type { get; set; }
        public NotificationChannel Channel { get; set; }
        
        public bool IsRead { get; set; }
        public DateTime SentAt { get; set; }
        public DateTime? ReadAt { get; set; }
        
        public User RecipientUser { get; set; }
    }
}
