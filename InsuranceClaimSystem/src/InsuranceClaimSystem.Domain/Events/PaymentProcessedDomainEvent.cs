using InsuranceClaimSystem.Domain.Common;
using InsuranceClaimSystem.Domain.Enums;

namespace InsuranceClaimSystem.Domain.Events;

public class PaymentProcessedDomainEvent : DomainEvent
{
    public Guid PaymentId { get; }
    public Guid ClaimId { get; }
    public decimal Amount { get; }
    public PaymentMethod PaymentMethod { get; }
    public ClaimPaymentStatus PaymentStatus { get; }
    public string? TransactionReference { get; }

    public PaymentProcessedDomainEvent(Guid paymentId, Guid claimId, decimal amount, PaymentMethod paymentMethod, ClaimPaymentStatus paymentStatus, string? transactionReference)
    {
        if (paymentId == Guid.Empty)
            throw new ArgumentException("PaymentId cannot be empty.", nameof(paymentId));
        if (claimId == Guid.Empty)
            throw new ArgumentException("ClaimId cannot be empty.", nameof(claimId));

        PaymentId = paymentId;
        ClaimId = claimId;
        Amount = amount;
        PaymentMethod = paymentMethod;
        PaymentStatus = paymentStatus;
        TransactionReference = transactionReference;
        EventType = "Payment.Processed";
    }
}