using MediatR;
using PayrollApp.Application.Common;

namespace PayrollApp.Application.Employees.Queries;

public record GetEmployeesQuery(
    bool? IsActive = null,
    int Page = 1,
    int PageSize = 50
) : IRequest<Result<EmployeesResponse>>;

public record EmployeesResponse(
    List<EmployeeDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);

public record EmployeeDto(
    Guid Id,
    string EmployeeCode,
    string FullName,
    string Email,
    string? Npwp,
    string PtkpStatus,
    DateOnly JoinDate,
    DateOnly? ResignDate,
    bool IsActive,
    List<SalaryComponentDto> SalaryComponents
);

// Made with Bob