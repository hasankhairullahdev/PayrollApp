using FluentValidation;
using PayrollApp.Application.Employees.Commands;

namespace PayrollApp.Application.Employees.Validators;

public class DeactivateEmployeeCommandValidator : AbstractValidator<DeactivateEmployeeCommand>
{
    public DeactivateEmployeeCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Employee ID is required");

        RuleFor(x => x.ResignDate)
            .NotEmpty().WithMessage("Resign date is required")
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("Resign date cannot be in the future");
    }
}

// Made with Bob