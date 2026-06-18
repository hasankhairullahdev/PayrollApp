using MediatR;
using Microsoft.AspNetCore.Mvc;
using PayrollApp.Application.Common;
using PayrollApp.Application.Employees.Commands;
using PayrollApp.Application.Employees.Queries;
using PayrollApp.Infrastructure.Security;

namespace PayrollApp.Api.Endpoints;

public static class EmployeeEndpoints
{
    public static void MapEmployeeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/employees")
            .WithTags("Employees")
            .WithOpenApi();

        // GET /api/employees - HR and Admin can view all employees
        group.MapGet("/", GetEmployees)
            .WithName("GetEmployees")
            .WithSummary("Get all employees with optional filtering")
            .RequireAuthorization(AuthorizationPolicies.RequireHROrAdmin)
            .Produces(401)
            .Produces(403);

        // GET /api/employees/{id} - HR and Admin can view employee details
        group.MapGet("/{id:guid}", GetEmployeeById)
            .WithName("GetEmployeeById")
            .WithSummary("Get employee by ID")
            .RequireAuthorization(AuthorizationPolicies.RequireHROrAdmin)
            .Produces(401)
            .Produces(403);

        // POST /api/employees - Only HR and Admin can create employees
        group.MapPost("/", CreateEmployee)
            .WithName("CreateEmployee")
            .WithSummary("Create a new employee")
            .RequireAuthorization(AuthorizationPolicies.RequireHROrAdmin)
            .Produces(401)
            .Produces(403);

        // PUT /api/employees/{id} - Only HR and Admin can update employees
        group.MapPut("/{id:guid}", UpdateEmployee)
            .WithName("UpdateEmployee")
            .WithSummary("Update an existing employee")
            .RequireAuthorization(AuthorizationPolicies.RequireHROrAdmin)
            .Produces(401)
            .Produces(403);

        // POST /api/employees/{id}/deactivate - Only HR and Admin can deactivate
        group.MapPost("/{id:guid}/deactivate", DeactivateEmployee)
            .WithName("DeactivateEmployee")
            .WithSummary("Deactivate an employee (resign)")
            .RequireAuthorization(AuthorizationPolicies.RequireHROrAdmin)
            .Produces(401)
            .Produces(403);
    }

    private static async Task<IResult> GetEmployees(
        [FromServices] IMediator mediator,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var query = new GetEmployeesQuery(isActive, page, pageSize);
        var result = await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(result.Error);
    }

    private static async Task<IResult> GetEmployeeById(
        [FromServices] IMediator mediator,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetEmployeeByIdQuery(id);
        var result = await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(result.Error);
    }

    private static async Task<IResult> CreateEmployee(
        [FromServices] IMediator mediator,
        [FromBody] CreateEmployeeCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/employees/{result.Value}", new { id = result.Value })
            : Results.BadRequest(result.Error);
    }

    private static async Task<IResult> UpdateEmployee(
        [FromServices] IMediator mediator,
        Guid id,
        [FromBody] UpdateEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateEmployeeCommand(
            id,
            request.FullName,
            request.Email,
            request.Npwp,
            request.PtkpStatus,
            request.SalaryComponents
        );

        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(new { success = true })
            : Results.BadRequest(result.Error);
    }

    private static async Task<IResult> DeactivateEmployee(
        [FromServices] IMediator mediator,
        Guid id,
        [FromBody] DeactivateEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new DeactivateEmployeeCommand(id, request.ResignDate);
        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(new { success = true })
            : Results.BadRequest(result.Error);
    }
}

public record UpdateEmployeeRequest(
    string FullName,
    string Email,
    string? Npwp,
    string PtkpStatus,
    List<SalaryComponentDto> SalaryComponents
);

public record DeactivateEmployeeRequest(DateOnly ResignDate);

// Made with Bob