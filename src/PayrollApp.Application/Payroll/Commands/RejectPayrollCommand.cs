using FluentValidation;
using Marten;
using MediatR;
using Microsoft.Extensions.Logging;
using PayrollApp.Application.Common;
using PayrollApp.Domain.Aggregates;

namespace PayrollApp.Application.Payroll.Commands;

/// <summary>
/// Command untuk reject PayrollRun yang sedang di-review.
/// Setelah rejected, payroll akan kembali ke status Draft.
/// </summary>
public record RejectPayrollCommand : IRequest<Result>
{
    public Guid PayrollRunId { get; init; }
    public string RejectedBy { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
}

/// <summary>
/// Validator untuk RejectPayrollCommand
/// </summary>
public class RejectPayrollCommandValidator : AbstractValidator<RejectPayrollCommand>
{
    public RejectPayrollCommandValidator()
    {
        RuleFor(x => x.PayrollRunId)
            .NotEmpty()
            .WithMessage("PayrollRunId is required");

        RuleFor(x => x.RejectedBy)
            .NotEmpty()
            .WithMessage("RejectedBy is required")
            .MaximumLength(100)
            .WithMessage("RejectedBy must not exceed 100 characters");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Reason is required")
            .MaximumLength(500)
            .WithMessage("Reason must not exceed 500 characters");
    }
}

/// <summary>
/// Handler untuk RejectPayrollCommand
/// </summary>
public class RejectPayrollCommandHandler : IRequestHandler<RejectPayrollCommand, Result>
{
    private readonly IDocumentStore _documentStore;
    private readonly ILogger<RejectPayrollCommandHandler> _logger;

    public RejectPayrollCommandHandler(
        IDocumentStore documentStore,
        ILogger<RejectPayrollCommandHandler> logger)
    {
        _documentStore = documentStore;
        _logger = logger;
    }

    public async Task<Result> Handle(RejectPayrollCommand request, CancellationToken cancellationToken)
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

            // Reject payroll
            payrollRun.Reject(request.RejectedBy, request.Reason);

            // Save events
            session.Events.Append(request.PayrollRunId, payrollRun.GetUncommittedEvents().ToArray());
            await session.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "PayrollRun {PayrollRunId} rejected by {RejectedBy}. Reason: {Reason}",
                request.PayrollRunId, request.RejectedBy, request.Reason);

            return Result.Success();
        }
        catch (Domain.Exceptions.InvalidPayrollStateException ex)
        {
            _logger.LogWarning(ex, "Invalid state for rejecting PayrollRun {PayrollRunId}", 
                request.PayrollRunId);
            
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting PayrollRun {PayrollRunId}", 
                request.PayrollRunId);
            
            return Result.Failure($"Failed to reject payroll: {ex.Message}");
        }
    }
}

// Made with Bob