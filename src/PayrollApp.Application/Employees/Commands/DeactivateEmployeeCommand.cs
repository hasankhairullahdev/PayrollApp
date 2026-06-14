using MediatR;
using PayrollApp.Application.Common;
using PayrollApp.Infrastructure.Repositories;

namespace PayrollApp.Application.Employees.Commands;

public record DeactivateEmployeeCommand(
    Guid Id,
    DateOnly ResignDate
) : IRequest<Result<bool>>;

public class DeactivateEmployeeCommandHandler : IRequestHandler<DeactivateEmployeeCommand, Result<bool>>
{
    private readonly IEmployeeRepository _employeeRepository;

    public DeactivateEmployeeCommandHandler(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    public async Task<Result<bool>> Handle(DeactivateEmployeeCommand request, CancellationToken cancellationToken)
    {
        // Load existing employee
        var employee = await _employeeRepository.GetByIdAsync(request.Id, cancellationToken);
        if (employee == null)
        {
            return Result.Failure<bool>($"Employee with ID {request.Id} not found");
        }

        if (!employee.IsActive)
        {
            return Result.Failure<bool>("Employee is already inactive");
        }

        // Use domain method to resign employee
        try
        {
            employee.Resign(request.ResignDate);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<bool>(ex.Message);
        }

        // Save updated employee
        await _employeeRepository.UpdateAsync(employee, cancellationToken);

        return Result.Success(true);
    }
}

// Made with Bob