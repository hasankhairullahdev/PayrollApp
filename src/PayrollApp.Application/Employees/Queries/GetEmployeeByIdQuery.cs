using MediatR;
using PayrollApp.Application.Common;
using PayrollApp.Infrastructure.Repositories;

namespace PayrollApp.Application.Employees.Queries;

public record GetEmployeeByIdQuery(Guid Id) : IRequest<Result<EmployeeDto>>;

public class GetEmployeeByIdQueryHandler : IRequestHandler<GetEmployeeByIdQuery, Result<EmployeeDto>>
{
    private readonly IEmployeeRepository _employeeRepository;

    public GetEmployeeByIdQueryHandler(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    public async Task<Result<EmployeeDto>> Handle(GetEmployeeByIdQuery request, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetByIdAsync(request.Id, cancellationToken);
        
        if (employee == null)
        {
            return Result.Failure<EmployeeDto>($"Employee with ID {request.Id} not found");
        }

        var employeeDto = new EmployeeDto(
            employee.Id,
            employee.EmployeeCode,
            employee.FullName,
            employee.Email,
            employee.Npwp,
            employee.PtkpStatus,
            employee.JoinDate,
            employee.ResignDate,
            employee.IsActive,
            employee.SalaryComponents.Select(c => new SalaryComponentDto(
                c.ComponentId,
                c.Name,
                c.Amount.Amount,
                c.Type.ToString(),
                c.EffectiveDate,
                null // EffectiveTo is not in domain model
            )).ToList()
        );

        return Result.Success(employeeDto);
    }
}

// Made with Bob