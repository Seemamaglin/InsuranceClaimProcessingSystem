using FluentValidation;
using InsuranceClaimSystem.Application.DTOs.Policies;

namespace InsuranceClaimSystem.Application.Validators;

public class PolicyNomineeRequestValidator : AbstractValidator<PolicyNomineeRequest>
{
    public PolicyNomineeRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Nominee full name is required")
            .MaximumLength(100).WithMessage("Nominee full name cannot exceed 100 characters");

        RuleFor(x => x.Relationship)
            .IsInEnum().WithMessage("Invalid relationship type");

        RuleFor(x => x.DateOfBirth)
            .LessThan(System.DateTime.Now).WithMessage("Date of birth must be in the past");

        RuleFor(x => x.ContactPhone)
            .NotEmpty().WithMessage("Contact phone is required")
            .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Invalid phone number format");

        RuleFor(x => x.ContactEmail)
            .NotEmpty().WithMessage("Contact email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.SharePercentage)
            .GreaterThan(0).WithMessage("Share percentage must be greater than 0")
            .LessThanOrEqualTo(100).WithMessage("Share percentage cannot exceed 100");
    }
}
