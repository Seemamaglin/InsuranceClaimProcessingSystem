using FluentValidation;
using InsuranceClaimSystem.Application.DTOs.Claims;
using InsuranceClaimSystem.Domain.Enums;

namespace InsuranceClaimSystem.Application.Validators;

public class UpdateClaimStatusRequestValidator : AbstractValidator<UpdateClaimStatusRequest>
{
    public UpdateClaimStatusRequestValidator()
    {
        RuleFor(x => x.NewStatus)
            .IsInEnum().WithMessage("Invalid claim status");

        RuleFor(x => x.ChangedByUserId)
            .NotEmpty().WithMessage("Changed by user ID is required")
            .NotEqual(Guid.Empty).WithMessage("Changed by user ID cannot be empty");
    }
}