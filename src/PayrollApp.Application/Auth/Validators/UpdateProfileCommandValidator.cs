using FluentValidation;
using PayrollApp.Application.Auth.Commands;

namespace PayrollApp.Application.Auth.Validators;

/// <summary>
/// Validator untuk UpdateProfileCommand
/// </summary>
public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.FullName)
            .NotEmpty()
            .WithMessage("Full name is required")
            .MinimumLength(3)
            .WithMessage("Full name must be at least 3 characters")
            .MaximumLength(100)
            .WithMessage("Full name must not exceed 100 characters");
    }
}

// Made with Bob