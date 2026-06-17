using FluentValidation;
using Marten;
using MediatR;
using Microsoft.Extensions.Logging;
using PayrollApp.Application.Common;
using PayrollApp.Domain.Aggregates;

namespace PayrollApp.Application.Payroll.Commands;

/// <summary>
/// Command untuk memulai review PayrollRun yang sudah calculated.
/// </summary>
public record StartReviewCommand : IRequest<Result>
{
    public Guid PayrollRunId { get; init; }
    public string ReviewedBy { get; init; } = string.Empty;
}

/// <summary>
/// Validator untuk StartReviewCommand
/// </summary>
public class StartReviewCommandValidator : AbstractValidator<StartReviewCommand>
{
    public StartReviewCommandValidator()
    {
        RuleFor(x => x.PayrollRunId)
            .NotEmpty()
            .WithMessage("PayrollRunId is required");

        RuleFor(x => x.ReviewedBy)
            .NotEmpty()
            .WithMessage("ReviewedBy is required")
            .MaximumLength(100)
            .WithMessage("ReviewedBy must not exceed 100 characters");
    }
}

/// <summary>
/// Handler untuk StartReviewCommand
/// </summary>
public class StartReviewCommandHandler : IRequestHandler<StartReviewCommand, Result>
{
    private readonly IDocumentStore _documentStore;
    private readonly ILogger<StartReviewCommandHandler> _logger;

    public StartReviewCommandHandler(
        IDocumentStore documentStore,
        ILogger<StartReviewCommandHandler> logger)
    {
        _documentStore = documentStore;
        _logger = logger;
    }

    public async Task<Result> Handle(StartReviewCommand request, CancellationToken cancellationToken)
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

            // Start review
            payrollRun.StartReview(request.ReviewedBy);

            // Save events
            session.Events.Append(request.PayrollRunId, payrollRun.GetUncommittedEvents().ToArray());
            await session.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "PayrollRun {PayrollRunId} review started by {ReviewedBy}",
                request.PayrollRunId, request.ReviewedBy);

            return Result.Success();
        }
        catch (Domain.Exceptions.InvalidPayrollStateException ex)
        {
            _logger.LogWarning(ex, "Invalid state for starting review of PayrollRun {PayrollRunId}", 
                request.PayrollRunId);
            
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting review of PayrollRun {PayrollRunId}", 
                request.PayrollRunId);
            
            return Result.Failure($"Failed to start review: {ex.Message}");
        }
    }
}

// Made with Bob