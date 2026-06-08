using FluentValidation;

namespace InsuranceClaimSystem.Application.Validators;

public class LoginRequestValidator : AbstractValidator<DTOs.Auth.LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.EmailOrUsername)
            .NotEmpty().WithMessage("Email or Username is required");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required");
    }
}