using FluentValidation;
using Marten;
using MediatR;
using Microsoft.Extensions.Logging;
using PayrollApp.Application.Common;
using PayrollApp.Domain.Aggregates;

namespace PayrollApp.Application.Payroll.Commands;

/// <summary>
/// Command untuk approve PayrollRun yang sudah di-review.
/// </summary>
public record ApprovePayrollCommand : IRequest<Result>
{
    public Guid PayrollRunId { get; init; }
    public string ApprovedBy { get; init; } = string.Empty;
    public string? Notes { get; init; }
}

/// <summary>
/// Validator untuk ApprovePayrollCommand
/// </summary>
public class ApprovePayrollCommandValidator : AbstractValidator<ApprovePayrollCommand>
{
    public ApprovePayrollCommandValidator()
    {
        RuleFor(x => x.PayrollRunId)
            .NotEmpty()
            .WithMessage("PayrollRunId is required");

        RuleFor(x => x.ApprovedBy)
            .NotEmpty()
            .WithMessage("ApprovedBy is required")
            .MaximumLength(100)
            .WithMessage("ApprovedBy must not exceed 100 characters");

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .WithMessage("Notes must not exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}

/// <summary>
/// Handler untuk ApprovePayrollCommand
/// </summary>
public class ApprovePayrollCommandHandler : IRequestHandler<ApprovePayrollCommand, Result>
{
    private readonly IDocumentStore _documentStore;
    private readonly ILogger<ApprovePayrollCommandHandler> _logger;

    public ApprovePayrollCommandHandler(
        IDocumentStore documentStore,
        ILogger<ApprovePayrollCommandHandler> logger)
    {
        _documentStore = documentStore;
        _logger = logger;
    }

    public async Task<Result> Handle(ApprovePayrollCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await using var session = _documentStore.LightweightSession();

            // Load PayrollRun aggregate
            var payrollRun = await session.Events.AggregateStreamAsync<PayrollRun>(
                request.PayrollRunId, 
                token: cancellationToken);

            if (payrollRun == null)
            {
                return Result.Failure($"PayrollRun {request.PayrollRunId} not found");
            }

            // Approve payroll
            payrollRun.Approve(request.ApprovedBy, request.Notes);

            // Save events
            session.Events.Append(request.PayrollRunId, payrollRun.GetUncommittedEvents().ToArray());
            await session.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "PayrollRun {PayrollRunId} approved by {ApprovedBy}",
                request.PayrollRunId, request.ApprovedBy);

            return Result.Success();
        }
        catch (Domain.Exceptions.InvalidPayrollStateException ex)
        {
            _logger.LogWarning(ex, "Invalid state for approving PayrollRun {PayrollRunId}", 
                request.PayrollRunId);
            
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving PayrollRun {PayrollRunId}", 
                request.PayrollRunId);
            
            return Result.Failure($"Failed to approve payroll: {ex.Message}");
        }
    }
}

// Made with Bob
