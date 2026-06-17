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

        // Update basic info using domain method
        employee.UpdateBasicInfo(request.FullName, request.Email, request.Npwp);

        // Update PTKP status using domain method
        employee.UpdatePtkpStatus(request.PtkpStatus);

        // Build new salary components list
        var newComponents = new List<SalaryComponent>();
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

            newComponents.Add(salaryComponent);
        }

        // Update salary components using domain method
        employee.UpdateSalaryComponents(newComponents);

        // Save updated employee
        await _employeeRepository.UpdateAsync(employee, cancellationToken);

        return Result.Success(true);
    }
}

// Made with Bob