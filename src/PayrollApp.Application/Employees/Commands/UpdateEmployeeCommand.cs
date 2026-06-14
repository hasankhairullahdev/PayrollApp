using MediatR;
using PayrollApp.Application.Common;
using PayrollApp.Domain.Enums;
using PayrollApp.Domain.ValueObjects;
using PayrollApp.Infrastructure.Repositories;

namespace PayrollApp.Application.Employees.Commands;

public record UpdateEmployeeCommand(
    Guid Id,
    string FullName,
    string Email,
    string? Npwp,
    string PtkpStatus,
    List<SalaryComponentDto> SalaryComponents
) : IRequest<Result<bool>>;

public class UpdateEmployeeCommandHandler : IRequestHandler<UpdateEmployeeCommand, Result<bool>>
{
    private readonly IEmployeeRepository _employeeRepository;

    public UpdateEmployeeCommandHandler(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    public async Task<Result<bool>> Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
    {
        // Load existing employee
        var employee = await _employeeRepository.GetByIdAsync(request.Id, cancellationToken);
        if (employee == null)
        {
            return Result.Failure<bool>($"Employee with ID {request.Id} not found");
        }

        // Update basic info - using reflection since domain doesn't expose setters
        // In a real scenario, you'd add proper domain methods for these updates
        var employeeType = employee.GetType();
        
        var fullNameProp = employeeType.GetProperty("FullName");
        fullNameProp?.SetValue(employee, request.FullName);
        
        var emailProp = employeeType.GetProperty("Email");
        emailProp?.SetValue(employee, request.Email);
        
        var npwpProp = employeeType.GetProperty("Npwp");
        npwpProp?.SetValue(employee, request.Npwp);

        // Update PTKP status using domain method
        employee.UpdatePtkpStatus(request.PtkpStatus);

        // Update salary components - remove all and add new ones
        var existingComponents = employee.SalaryComponents.ToList();
        foreach (var component in existingComponents)
        {
            employee.RemoveSalaryComponent(component.ComponentId);
        }

        foreach (var componentDto in request.SalaryComponents)
        {
            if (!Enum.TryParse<SalaryComponentType>(componentDto.Type, out var componentType))
            {
                return Result.Failure<bool>($"Invalid salary component type: {componentDto.Type}");
            }

            var salaryComponent = new SalaryComponent(
                Guid.NewGuid(),
                componentDto.Name,
                new Money(componentDto.Amount),
                componentType,
                componentDto.EffectiveFrom
            );

            employee.AddSalaryComponent(salaryComponent);
        }

        // Save updated employee
        await _employeeRepository.UpdateAsync(employee, cancellationToken);

        return Result.Success(true);
    }
}

// Made with Bob