using MediatR;
using Microsoft.AspNetCore.Mvc;
using PayrollApp.Application.Common;
using PayrollApp.Application.Payroll.Commands;
using PayrollApp.Application.Payroll.Queries;

namespace PayrollApp.Api.Endpoints;

/// <summary>
/// Minimal API endpoints untuk Payroll operations
/// </summary>
public static class PayrollEndpoints
{
    public static void MapPayrollEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/payroll")
            .WithTags("Payroll")
            .WithOpenApi();

        // GET /api/payroll - List payroll runs dengan pagination & filtering
        group.MapGet("/", GetPayrollRuns)
            .WithName("GetPayrollRuns")
            .WithSummary("Get list of payroll runs")
            .Produces<PayrollRunsResponse>(200)
            .Produces<ProblemDetails>(400);

        // GET /api/payroll/{id} - Get payroll run detail
        group.MapGet("/{id:guid}", GetPayrollRunDetail)
            .WithName("GetPayrollRunDetail")
            .WithSummary("Get payroll run detail with line items")
            .Produces<PayrollRunDetailResponse>(200)
            .Produces<ProblemDetails>(404);

        // POST /api/payroll - Create new payroll run
        group.MapPost("/", CreatePayrollRun)
            .WithName("CreatePayrollRun")
            .WithSummary("Create new payroll run and trigger calculation")
            .Produces<Guid>(201)
            .Produces<ProblemDetails>(400);

        // POST /api/payroll/{id}/approve - Approve payroll run
        group.MapPost("/{id:guid}/approve", ApprovePayrollRun)
            .WithName("ApprovePayrollRun")
            .WithSummary("Approve payroll run after review")
            .Produces(200)
            .Produces<ProblemDetails>(400)
            .Produces<ProblemDetails>(404);

        // POST /api/payroll/{id}/lock - Lock payroll run
        group.MapPost("/{id:guid}/lock", LockPayrollRun)
            .WithName("LockPayrollRun")
            .WithSummary("Lock payroll run and trigger payslip generation")
            .Produces(200)
            .Produces<ProblemDetails>(400)
            .Produces<ProblemDetails>(404);
    }

    private static async Task<IResult> GetPayrollRuns(
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] string? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken = default)
    {
        var query = new GetPayrollRunsQuery
        {
            Year = year,
            Month = month,
            Status = status,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(CreateProblemDetails("Failed to retrieve payroll runs", result.Error));
    }

    private static async Task<IResult> GetPayrollRunDetail(
        Guid id,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken = default)
    {
        var query = new GetPayrollRunDetailQuery { PayrollRunId = id };
        var result = await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(CreateProblemDetails("Payroll run not found", result.Error));
    }

    private static async Task<IResult> CreatePayrollRun(
        [FromBody] CreatePayrollRunRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken = default)
    {
        var command = new CreatePayrollRunCommand
        {
            Month = request.Month,
            Year = request.Year,
            CreatedBy = request.CreatedBy
        };

        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/payroll/{result.Value}", result.Value)
            : Results.BadRequest(CreateProblemDetails("Failed to create payroll run", result.Error));
    }

    private static async Task<IResult> ApprovePayrollRun(
        Guid id,
        [FromBody] ApprovePayrollRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken = default)
    {
        var command = new ApprovePayrollCommand
        {
            PayrollRunId = id,
            ApprovedBy = request.ApprovedBy,
            Notes = request.Notes
        };

        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok()
            : Results.BadRequest(CreateProblemDetails("Failed to approve payroll run", result.Error));
    }

    private static async Task<IResult> LockPayrollRun(
        Guid id,
        [FromBody] LockPayrollRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken = default)
    {
        var command = new LockPayrollCommand
        {
            PayrollRunId = id,
            LockedBy = request.LockedBy
        };

        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok()
            : Results.BadRequest(CreateProblemDetails("Failed to lock payroll run", result.Error));
    }

    private static ProblemDetails CreateProblemDetails(string title, string detail)
    {
        return new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = StatusCodes.Status400BadRequest
        };
    }
}

// Request DTOs
public record CreatePayrollRunRequest(int Month, int Year, string CreatedBy);
public record ApprovePayrollRequest(string ApprovedBy, string? Notes);
public record LockPayrollRequest(string LockedBy);

// Made with Bob
