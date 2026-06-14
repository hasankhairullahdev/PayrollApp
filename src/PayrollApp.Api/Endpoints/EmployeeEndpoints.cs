using MediatR;
using Microsoft.AspNetCore.Mvc;
using PayrollApp.Application.Common;
using PayrollApp.Application.Employees.Commands;
using PayrollApp.Application.Employees.Queries;

namespace PayrollApp.Api.Endpoints;

public static class EmployeeEndpoints
{
    public static void MapEmployeeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/employees")
            .WithTags("Employees")
            .WithOpenApi();

        group.MapGet("/", GetEmployees)
            .WithName("GetEmployees")
            .WithSummary("Get all employees with optional filtering");

        group.MapGet("/{id:guid}", GetEmployeeById)
            .WithName("GetEmployeeById")
            .WithSummary("Get employee by ID");

        group.MapPost("/", CreateEmployee)
            .WithName("CreateEmployee")
            .WithSummary("Create a new employee");

        group.MapPut("/{id:guid}", UpdateEmployee)
            .WithName("UpdateEmployee")
            .WithSummary("Update an existing employee");

        group.MapPost("/{id:guid}/deactivate", DeactivateEmployee)
            .WithName("DeactivateEmployee")
            .WithSummary("Deactivate an employee (resign)");
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