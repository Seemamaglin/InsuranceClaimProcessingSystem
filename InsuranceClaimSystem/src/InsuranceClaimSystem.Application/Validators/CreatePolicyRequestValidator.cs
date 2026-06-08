using FluentValidation;
using InsuranceClaimSystem.Application.DTOs.Policies;
using InsuranceClaimSystem.Domain.Enums;

namespace InsuranceClaimSystem.Application.Validators;

public class CreatePolicyRequestValidator : AbstractValidator<CreatePolicyRequest>
{
    public CreatePolicyRequestValidator()
    {
        RuleFor(x => x.PolicyHolderId)
            .NotEmpty().WithMessage("Policy holder ID is required")
            .NotEqual(Guid.Empty).WithMessage("Policy holder ID cannot be empty");

        RuleFor(x => x.PolicyTypeId)
            .NotEmpty().WithMessage("Policy type ID is required")
            .NotEqual(Guid.Empty).WithMessage("Policy type ID cannot be empty");

        RuleFor(x => x.StartDate)
            .GreaterThanOrEqualTo(DateTime.Now.Date).WithMessage("Start date must be today or in the future");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate).WithMessage("End date must be after start date");

        RuleFor(x => x.CoverageAmount)
            .GreaterThan(0).WithMessage("Coverage amount must be greater than 0");

        RuleFor(x => x.PremiumAmount)
            .GreaterThan(0).WithMessage("Premium amount must be greater than 0");

        RuleFor(x => x.PremiumFrequency)
            .IsInEnum().WithMessage("Invalid premium frequency");
    }
}