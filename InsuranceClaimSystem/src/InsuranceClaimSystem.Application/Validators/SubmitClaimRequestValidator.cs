using FluentValidation;
using InsuranceClaimSystem.Application.DTOs.Claims;

namespace InsuranceClaimSystem.Application.Validators;

public class SubmitClaimRequestValidator : AbstractValidator<SubmitClaimRequest>
{
    public SubmitClaimRequestValidator()
    {
        RuleFor(x => x.PolicyId)
            .NotEmpty().WithMessage("Policy ID is required")
            .NotEqual(Guid.Empty).WithMessage("Policy ID cannot be empty");

        RuleFor(x => x.ClaimTypeId)
            .NotEmpty().WithMessage("Claim type ID is required")
            .NotEqual(Guid.Empty).WithMessage("Claim type ID cannot be empty");

        RuleFor(x => x.ClaimedAmount)
            .GreaterThan(0).WithMessage("Claimed amount must be greater than 0");

        RuleFor(x => x.IncidentDate)
            .LessThanOrEqualTo(DateTime.Now).WithMessage("Incident date cannot be in the future");

        RuleFor(x => x.IncidentDescription)
            .NotEmpty().WithMessage("Incident description is required")
            .NotEqual("string", StringComparer.OrdinalIgnoreCase).WithMessage("Please provide a real incident description, not the default 'string'")
            .MaximumLength(2000).WithMessage("Incident description must not exceed 2000 characters");
    }
}