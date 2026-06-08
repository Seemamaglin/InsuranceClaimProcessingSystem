using InsuranceClaimSystem.Domain.Enums;

namespace InsuranceClaimSystem.Application.DTOs.Claims;

public class ClaimPaymentDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public ClaimPaymentStatus PaymentStatus { get; set; }
    public DateTime? ProcessedAt { get; set; }
}