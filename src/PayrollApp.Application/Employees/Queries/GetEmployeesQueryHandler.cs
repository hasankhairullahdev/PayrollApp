using MediatR;
using PayrollApp.Application.Common;
using PayrollApp.Infrastructure.Repositories;

namespace PayrollApp.Application.Employees.Queries;

public class GetEmployeesQueryHandler : IRequestHandler<GetEmployeesQuery, Result<EmployeesResponse>>
{
    private readonly IEmployeeRepository _employeeRepository;

    public GetEmployeesQueryHandler(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    public async Task<Result<EmployeesResponse>> Handle(GetEmployeesQuery request, CancellationToken cancellationToken)
    {
        var (employees, totalCount) = await _employeeRepository.GetEmployeesAsync(
            request.IsActive,
            request.Page,
            request.PageSize,
            cancellationToken
        );

        var employeeDtos = employees.Select(e => new EmployeeDto(
            e.Id,
            e.EmployeeCode,
            e.FullName,
            e.Email,
            e.Npwp,
            e.PtkpStatus,
            e.JoinDate,
            e.ResignDate,
            e.IsActive,
            e.SalaryComponents.Select(c => new SalaryComponentDto(
                c.ComponentId,
                c.Name,
                c.Amount.Amount,
                c.Type.ToString(),
                c.EffectiveDate,
                null // EffectiveTo is not in domain model, set to null
            )).ToList()
        )).ToList();

        var response = new EmployeesResponse(employeeDtos, totalCount, request.Page, request.PageSize);
        return Result<EmployeesResponse>.Success(response);
    }
}

// Made with Bob