using FluentValidation;
using PayrollApp.Application.Auth.Commands;

namespace PayrollApp.Application.Auth.Validators;

/// <summary>
/// Validator untuk LoginCommand
/// </summary>
public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required");
    }
}

// Made with Bob
