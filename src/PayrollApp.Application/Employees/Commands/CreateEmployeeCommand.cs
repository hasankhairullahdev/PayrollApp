using MediatR;
using PayrollApp.Application.Common;
using PayrollApp.Domain.Aggregates;
using PayrollApp.Domain.Enums;
using PayrollApp.Domain.ValueObjects;
using PayrollApp.Infrastructure.Repositories;

namespace PayrollApp.Application.Employees.Commands;

public record CreateEmployeeCommand(
    string EmployeeCode,
    string FullName,
    string Email,
    string? Npwp,
    string PtkpStatus,
    DateOnly JoinDate,
    List<SalaryComponentDto> SalaryComponents
) : IRequest<Result<Guid>>;

public class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, Result<Guid>>
{
    private readonly IEmployeeRepository _employeeRepository;

    public CreateEmployeeCommandHandler(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    public async Task<Result<Guid>> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        // Check if employee code already exists
        var existingEmployee = await _employeeRepository.GetByCodeAsync(request.EmployeeCode, cancellationToken);
        if (existingEmployee != null)
        {
            return Result.Failure<Guid>($"Employee with code {request.EmployeeCode} already exists");
        }

        // Create employee aggregate
        var employee = new Employee(
            Guid.NewGuid(),
            request.EmployeeCode,
            request.FullName,
            request.Email,
            request.Npwp,
            request.PtkpStatus,
            request.JoinDate
        );

        // Add salary components
        foreach (var component in request.SalaryComponents)
        {
            if (!Enum.TryParse<SalaryComponentType>(component.Type, out var componentType))
            {
                return Result.Failure<Guid>($"Invalid salary component type: {component.Type}");
            }

            var salaryComponent = new SalaryComponent(
                Guid.NewGuid(),
                component.Name,
                new Money(component.Amount),
                componentType,
                component.EffectiveFrom
            );

            employee.AddSalaryComponent(salaryComponent);
        }

        // Save to repository
        await _employeeRepository.SaveAsync(employee, cancellationToken);

        return Result.Success(employee.Id);
    }
}

// Made with Bob