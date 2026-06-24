using FluentValidation;
using Marten;
using MediatR;
using Microsoft.Extensions.Logging;
using PayrollApp.Application.Common;
using PayrollApp.Domain.Aggregates;

namespace PayrollApp.Application.Payroll.Commands;

/// <summary>
/// Command untuk konfirmasi bahwa transfer payroll sudah dilakukan.
/// </summary>
public record ConfirmDisbursementCommand : IRequest<Result>
{
    public Guid PayrollRunId { get; init; }
    public string ConfirmedBy { get; init; } = string.Empty;
}

/// <summary>
/// Validator untuk ConfirmDisbursementCommand
/// </summary>
public class ConfirmDisbursementCommandValidator : AbstractValidator<ConfirmDisbursementCommand>
{
    public ConfirmDisbursementCommandValidator()
    {
        RuleFor(x => x.PayrollRunId)
            .NotEmpty()
            .WithMessage("PayrollRunId is required");

        RuleFor(x => x.ConfirmedBy)
            .NotEmpty()
            .WithMessage("ConfirmedBy is required")
            .MaximumLength(100)
            .WithMessage("ConfirmedBy must not exceed 100 characters");
    }
}

/// <summary>
/// Handler untuk ConfirmDisbursementCommand
/// </summary>
public class ConfirmDisbursementCommandHandler : IRequestHandler<ConfirmDisbursementCommand, Result>
{
    private readonly IDocumentStore _documentStore;
    private readonly ILogger<ConfirmDisbursementCommandHandler> _logger;

    public ConfirmDisbursementCommandHandler(
        IDocumentStore documentStore,
        ILogger<ConfirmDisbursementCommandHandler> logger)
    {
        _documentStore = documentStore;
        _logger = logger;
    }

    public async Task<Result> Handle(ConfirmDisbursementCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await using var session = _documentStore.LightweightSession();

            var events = await session.Events.FetchStreamAsync(request.PayrollRunId, token: cancellationToken);

            if (events == null || !events.Any())
            {
                return Result.Failure($"PayrollRun {request.PayrollRunId} not found");
            }

            var payrollRun = PayrollRun.FromEvents(events.Select(e => e.Data));

            payrollRun.ConfirmDisbursement(request.ConfirmedBy);

            session.Events.Append(request.PayrollRunId, payrollRun.GetUncommittedEvents().ToArray());
            await session.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "PayrollRun {PayrollRunId} disbursement confirmed by {ConfirmedBy}",
                request.PayrollRunId,
                request.ConfirmedBy);

            return Result.Success();
        }
        catch (Domain.Exceptions.InvalidPayrollStateException ex)
        {
            _logger.LogWarning(ex, "Invalid state for confirming disbursement of PayrollRun {PayrollRunId}",
                request.PayrollRunId);

            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming disbursement of PayrollRun {PayrollRunId}",
                request.PayrollRunId);

            return Result.Failure($"Failed to confirm disbursement: {ex.Message}");
        }
    }
}

// Made with Bob
