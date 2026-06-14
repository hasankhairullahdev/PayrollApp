using FluentValidation;
using PayrollApp.Application.Common;
using PayrollApp.Application.Employees.Commands;

namespace PayrollApp.Application.Employees.Validators;

public class UpdateEmployeeCommandValidator : AbstractValidator<UpdateEmployeeCommand>
{
    public UpdateEmployeeCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Employee ID is required");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required")
            .MaximumLength(100).WithMessage("Full name must not exceed 100 characters");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(100).WithMessage("Email must not exceed 100 characters");

        RuleFor(x => x.Npwp)
            .MaximumLength(20).WithMessage("NPWP must not exceed 20 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Npwp));

        RuleFor(x => x.PtkpStatus)
            .NotEmpty().WithMessage("PTKP status is required")
            .Must(BeValidPtkpStatus).WithMessage("Invalid PTKP status. Valid values: TK/0, TK/1, K/0, K/1, K/2, K/3");

        RuleFor(x => x.SalaryComponents)
            .NotEmpty().WithMessage("At least one salary component is required")
            .Must(HaveBasicSalary).WithMessage("Basic salary component is required");

        RuleForEach(x => x.SalaryComponents)
            .ChildRules(component =>
            {
                component.RuleFor(c => c.Name)
                    .NotEmpty().WithMessage("Component name is required")
                    .MaximumLength(100).WithMessage("Component name must not exceed 100 characters");

                component.RuleFor(c => c.Amount)
                    .GreaterThan(0).WithMessage("Component amount must be greater than zero");

                component.RuleFor(c => c.Type)
                    .NotEmpty().WithMessage("Component type is required")
                    .Must(BeValidComponentType).WithMessage("Invalid component type. Valid values: BasicSalary, FixedAllowance, VariableAllowance, Deduction");

                component.RuleFor(c => c.EffectiveFrom)
                    .NotEmpty().WithMessage("Effective from date is required");
            });
    }

    private bool BeValidPtkpStatus(string ptkpStatus)
    {
        var validStatuses = new[] { "TK/0", "TK/1", "K/0", "K/1", "K/2", "K/3" };
        return validStatuses.Contains(ptkpStatus);
    }

    private bool HaveBasicSalary(List<SalaryComponentDto> components)
    {
        return components.Any(c => c.Type == "BasicSalary");
    }

    private bool BeValidComponentType(string type)
    {
        var validTypes = new[] { "BasicSalary", "FixedAllowance", "VariableAllowance", "Deduction" };
        return validTypes.Contains(type);
    }
}

// Made with Bob