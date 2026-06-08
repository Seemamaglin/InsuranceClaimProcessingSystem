using FluentValidation;
using InsuranceClaimSystem.Application.DTOs.Nominees;
using InsuranceClaimSystem.Domain.Enums;

namespace InsuranceClaimSystem.Application.Validators;

public class NomineeRequestValidator : AbstractValidator<NomineeRequest>
{
    public NomineeRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required")
            .MaximumLength(200).WithMessage("Full name must not exceed 200 characters");

        RuleFor(x => x.Relationship)
            .IsInEnum().WithMessage("Invalid relationship type");

        RuleFor(x => x.DateOfBirth)
            .LessThan(DateTime.Now).WithMessage("Date of birth must be in the past");

        RuleFor(x => x.SharePercentage)
            .GreaterThan(0).WithMessage("Share percentage must be greater than 0")
            .LessThanOrEqualTo(100).WithMessage("Share percentage must be less than or equal to 100");
    }
}