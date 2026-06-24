using FluentValidation;
using Marten;
using MediatR;
using Microsoft.Extensions.Logging;
using PayrollApp.Application.Common;
using PayrollApp.Domain.Aggregates;
using PayrollApp.Infrastructure.ReadModels;

namespace PayrollApp.Application.Payroll.Commands;

/// <summary>
/// Command untuk memulai proses disbursement payroll yang sudah locked.
/// </summary>
public record InitiateDisbursementCommand : IRequest<Result>
{
    public Guid PayrollRunId { get; init; }
    public string BankName { get; init; } = string.Empty;
}

/// <summary>
/// Validator untuk InitiateDisbursementCommand
/// </summary>
public class InitiateDisbursementCommandValidator : AbstractValidator<InitiateDisbursementCommand>
{
    public InitiateDisbursementCommandValidator()
    {
        RuleFor(x => x.PayrollRunId)
            .NotEmpty()
            .WithMessage("PayrollRunId is required");

        RuleFor(x => x.BankName)
            .NotEmpty()
            .WithMessage("BankName is required")
            .MaximumLength(50)
            .WithMessage("BankName must not exceed 50 characters");
    }
}

/// <summary>
/// Handler untuk InitiateDisbursementCommand
/// </summary>
public class InitiateDisbursementCommandHandler : IRequestHandler<InitiateDisbursementCommand, Result>
{
    private readonly IDocumentStore _documentStore;
    private readonly ILogger<InitiateDisbursementCommandHandler> _logger;

    public InitiateDisbursementCommandHandler(
        IDocumentStore documentStore,
        ILogger<InitiateDisbursementCommandHandler> logger)
    {
        _documentStore = documentStore;
        _logger = logger;
    }

    public async Task<Result> Handle(InitiateDisbursementCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await using var session = _documentStore.LightweightSession();

            var summary = await session.LoadAsync<PayrollRunSummary>(request.PayrollRunId, cancellationToken);
            if (summary == null)
            {
                return Result.Failure($"PayrollRun {request.PayrollRunId} not found");
            }

            if (summary.Status != Domain.Enums.PayrollStatus.Locked)
            {
                return Result.Failure($"Cannot initiate disbursement. Payroll must be in Locked status, current status: {summary.Status}");
            }

            var lineItems = await session.Query<PayrollLineItem>()
                .Where(x => x.PayrollRunId == request.PayrollRunId)
                .ToListAsync(cancellationToken);

            if (lineItems.Count == 0)
            {
                return Result.Failure("No line items found for this payroll run");
            }

            var events = await session.Events.FetchStreamAsync(request.PayrollRunId, token: cancellationToken);
            if (events == null || !events.Any())
            {
                return Result.Failure($"PayrollRun {request.PayrollRunId} not found");
            }

            var payrollRun = PayrollRun.FromEvents(events.Select(e => e.Data));
            var bankFileUrl = Path.Combine(
                "bank-files",
                summary.Year.ToString(),
                summary.Month.ToString("D2"),
                $"{request.BankName.ToLowerInvariant()}-{request.PayrollRunId}.txt");

            payrollRun.InitiateDisbursement(bankFileUrl, request.BankName);

            session.Events.Append(request.PayrollRunId, payrollRun.GetUncommittedEvents().ToArray());
            await session.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "PayrollRun {PayrollRunId} disbursement initiated for bank {BankName}",
                request.PayrollRunId,
                request.BankName);

            return Result.Success();
        }
        catch (Domain.Exceptions.InvalidPayrollStateException ex)
        {
            _logger.LogWarning(ex, "Invalid state for initiating disbursement of PayrollRun {PayrollRunId}",
                request.PayrollRunId);

            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating disbursement of PayrollRun {PayrollRunId}",
                request.PayrollRunId);

            return Result.Failure($"Failed to initiate disbursement: {ex.Message}");
        }
    }
}

// Made with Bob
