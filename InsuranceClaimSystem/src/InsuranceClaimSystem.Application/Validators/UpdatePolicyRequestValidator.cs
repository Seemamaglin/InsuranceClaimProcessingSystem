using FluentValidation;
using InsuranceClaimSystem.Application.DTOs.Policies;

namespace InsuranceClaimSystem.Application.Validators;

public class UpdatePolicyRequestValidator : AbstractValidator<UpdatePolicyRequest>
{
    public UpdatePolicyRequestValidator()
    {
        RuleFor(x => x.PolicyId)
            .NotEmpty().WithMessage("Policy ID is required");

        RuleFor(x => x.CoverageAmount)
            .GreaterThan(0).When(x => x.CoverageAmount.HasValue).WithMessage("Coverage amount must be greater than 0")
            .InclusiveBetween(10000, 10000000).When(x => x.CoverageAmount.HasValue).WithMessage("Coverage amount must be between 10,000 and 10,000,000");

        RuleFor(x => x.PremiumAmount)
            .GreaterThan(0).When(x => x.PremiumAmount.HasValue).WithMessage("Premium amount must be greater than 0")
            .LessThan(x => x.CoverageAmount).When(x => x.PremiumAmount.HasValue && x.CoverageAmount.HasValue)
            .WithMessage("Premium amount must be less than the coverage amount");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(DateTime.UtcNow.Date).When(x => x.EndDate.HasValue).WithMessage("End date must be today or in the future");
    }
}