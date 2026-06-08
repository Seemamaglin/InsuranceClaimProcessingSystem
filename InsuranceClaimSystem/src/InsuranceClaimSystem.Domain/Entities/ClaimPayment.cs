using InsuranceClaimSystem.Domain.Common;
using InsuranceClaimSystem.Domain.Enums;

namespace InsuranceClaimSystem.Domain.Entities
{
    public class ClaimPayment : BaseEntity
    {
        public Guid ClaimId { get; set; }
        public decimal Amount { get; set; }
        public PaymentRecipientType RecipientType { get; set; }
        public string? RecipientName { get; set; }
        public string? RecipientAccountNumber { get; set; }
        public string? RecipientBankName { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public ClaimPaymentStatus PaymentStatus { get; set; }
        public string? StripePaymentIntentId { get; set; }
        public Guid IdempotencyKey { get; set; }
        public DateTime? ProcessedAt { get; set; }

        public Claim Claim { get; set; } = default!;
    }
}