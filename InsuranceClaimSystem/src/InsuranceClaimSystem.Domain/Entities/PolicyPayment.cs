using InsuranceClaimSystem.Domain.Common;
using InsuranceClaimSystem.Domain.Enums;

namespace InsuranceClaimSystem.Domain.Entities;

public class PolicyPayment : BaseEntity  // Inherits full BaseEntity (Id, CreatedAt, UpdatedAt, IsDeleted)
{
    public Guid PolicyId { get; set; }

    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }

    public PolicyPaymentStatus Status { get; set; }
    public string? StripePaymentIntentId { get; set; }
    public string? TransactionId { get; set; }

    // Navigation
    public Policy Policy { get; set; } = null!;
}