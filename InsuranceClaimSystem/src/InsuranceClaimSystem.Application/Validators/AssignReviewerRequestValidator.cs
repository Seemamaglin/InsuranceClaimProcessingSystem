using FluentValidation;
using InsuranceClaimSystem.Application.DTOs.Claims;

namespace InsuranceClaimSystem.Application.Validators;

public class AssignReviewerRequestValidator : AbstractValidator<AssignReviewerRequest>
{
    public AssignReviewerRequestValidator()
    {
        RuleFor(x => x.ClaimId)
            .NotEmpty().WithMessage("Claim ID is required")
            .NotEqual(Guid.Empty).WithMessage("Claim ID cannot be empty");

        RuleFor(x => x.ReviewerId)
            .NotEmpty().WithMessage("Reviewer ID is required")
            .NotEqual(Guid.Empty).WithMessage("Reviewer ID cannot be empty");

        RuleFor(x => x.AssignedByUserId)
            .NotEmpty().WithMessage("Assigned by user ID is required")
            .NotEqual(Guid.Empty).WithMessage("Assigned by user ID cannot be empty");
    }
}