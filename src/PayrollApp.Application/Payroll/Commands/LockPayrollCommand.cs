using FluentValidation;
using Hangfire;
using Marten;
using MediatR;
using Microsoft.Extensions.Logging;
using PayrollApp.Application.Common;
using PayrollApp.Domain.Aggregates;
using PayrollApp.Infrastructure.Jobs;

namespace PayrollApp.Application.Payroll.Commands;

/// <summary>
/// Command untuk lock PayrollRun setelah approved.
/// Setelah locked, payroll tidak bisa diubah dan payslip generation akan di-trigger.
/// </summary>
public record LockPayrollCommand : IRequest<Result>
{
    public Guid PayrollRunId { get; init; }
    public string LockedBy { get; init; } = string.Empty;
}

/// <summary>
/// Validator untuk LockPayrollCommand
/// </summary>
public class LockPayrollCommandValidator : AbstractValidator<LockPayrollCommand>
{
    public LockPayrollCommandValidator()
    {
        RuleFor(x => x.PayrollRunId)
            .NotEmpty()
            .WithMessage("PayrollRunId is required");

        RuleFor(x => x.LockedBy)
            .NotEmpty()
            .WithMessage("LockedBy is required")
            .MaximumLength(100)
            .WithMessage("LockedBy must not exceed 100 characters");
    }
}

/// <summary>
/// Handler untuk LockPayrollCommand
/// </summary>
public class LockPayrollCommandHandler : IRequestHandler<LockPayrollCommand, Result>
{
    private readonly IDocumentStore _documentStore;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<LockPayrollCommandHandler> _logger;

    public LockPayrollCommandHandler(
        IDocumentStore documentStore,
        IBackgroundJobClient backgroundJobClient,
        ILogger<LockPayrollCommandHandler> logger)
    {
        _documentStore = documentStore;
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    public async Task<Result> Handle(LockPayrollCommand request, CancellationToken cancellationToken)
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

            // Lock payroll
            payrollRun.Lock(request.LockedBy);

            // Save events
            session.Events.Append(request.PayrollRunId, payrollRun.GetUncommittedEvents().ToArray());
            await session.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "PayrollRun {PayrollRunId} locked by {LockedBy}",
                request.PayrollRunId, request.LockedBy);

            // Enqueue background job untuk payslip generation
            _backgroundJobClient.Enqueue<PayslipGenerationJob>(
                job => job.ExecuteAsync(request.PayrollRunId, JobCancellationToken.Null));

            _logger.LogInformation(
                "Payslip generation job enqueued for PayrollRun {PayrollRunId}",
                request.PayrollRunId);

            return Result.Success();
        }
        catch (Domain.Exceptions.InvalidPayrollStateException ex)
        {
            _logger.LogWarning(ex, "Invalid state for locking PayrollRun {PayrollRunId}", 
                request.PayrollRunId);
            
            return Result.Failure(ex.Message);
        }
        catch (Domain.Exceptions.PayrollAlreadyLockedException ex)
        {
            _logger.LogWarning(ex, "PayrollRun {PayrollRunId} already locked", 
                request.PayrollRunId);
            
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error locking PayrollRun {PayrollRunId}", 
                request.PayrollRunId);
            
            return Result.Failure($"Failed to lock payroll: {ex.Message}");
        }
    }
}

// Made with Bob
