using FluentValidation;
using InsuranceClaimSystem.Application.DTOs.Addresses;

namespace InsuranceClaimSystem.Application.Validators;

public class AddressRequestValidator : AbstractValidator<AddressRequest>
{
    public AddressRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required")
            .NotEqual(Guid.Empty).WithMessage("User ID cannot be empty");

        RuleFor(x => x.Street)
            .NotEmpty().WithMessage("Street is required");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required");

        RuleFor(x => x.State)
            .NotEmpty().WithMessage("State is required");

        RuleFor(x => x.PostalCode)
            .NotEmpty().WithMessage("Postal code is required");
    }
}