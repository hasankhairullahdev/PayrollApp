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
/// Command untuk membuat PayrollRun baru dan trigger calculation job.
/// </summary>
public record CreatePayrollRunCommand : IRequest<Result<Guid>>
{
    public int Month { get; init; }
    public int Year { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
}

/// <summary>
/// Validator untuk CreatePayrollRunCommand
/// </summary>
public class CreatePayrollRunCommandValidator : AbstractValidator<CreatePayrollRunCommand>
{
    public CreatePayrollRunCommandValidator()
    {
        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12)
            .WithMessage("Month must be between 1 and 12");

        RuleFor(x => x.Year)
            .InclusiveBetween(2020, 2100)
            .WithMessage("Year must be between 2020 and 2100");

        RuleFor(x => x.CreatedBy)
            .NotEmpty()
            .WithMessage("CreatedBy is required")
            .MaximumLength(100)
            .WithMessage("CreatedBy must not exceed 100 characters");
    }
}

/// <summary>
/// Handler untuk CreatePayrollRunCommand
/// </summary>
public class CreatePayrollRunCommandHandler : IRequestHandler<CreatePayrollRunCommand, Result<Guid>>
{
    private readonly IDocumentStore _documentStore;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<CreatePayrollRunCommandHandler> _logger;

    public CreatePayrollRunCommandHandler(
        IDocumentStore documentStore,
        IBackgroundJobClient backgroundJobClient,
        ILogger<CreatePayrollRunCommandHandler> logger)
    {
        _documentStore = documentStore;
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CreatePayrollRunCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await using var session = _documentStore.LightweightSession();

            // Check if payroll run already exists for this period
            var existingPayrollRun = await session.Query<Infrastructure.ReadModels.PayrollRunSummary>()
                .Where(x => x.Month == request.Month && x.Year == request.Year)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingPayrollRun != null)
            {
                return Result.Failure<Guid>(
                    $"Payroll run for {request.Month}/{request.Year} already exists");
            }

            // Create new PayrollRun aggregate
            var payrollRun = PayrollRun.Create(request.Month, request.Year, request.CreatedBy);
            
            // Start calculation (change status to Calculating)
            payrollRun.StartCalculation();

            // Save events
            session.Events.Append(payrollRun.Id, payrollRun.GetUncommittedEvents().ToArray());
            await session.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "PayrollRun {PayrollRunId} created for period {Month}/{Year} by {CreatedBy}",
                payrollRun.Id, request.Month, request.Year, request.CreatedBy);

            // Enqueue background job untuk calculation
            _backgroundJobClient.Enqueue<PayrollCalculationJob>(
                job => job.ExecuteAsync(payrollRun.Id, JobCancellationToken.Null));

            _logger.LogInformation(
                "Payroll calculation job enqueued for PayrollRun {PayrollRunId}",
                payrollRun.Id);

            return Result.Success(payrollRun.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payroll run for period {Month}/{Year}", 
                request.Month, request.Year);
            
            return Result.Failure<Guid>($"Failed to create payroll run: {ex.Message}");
        }
    }
}

// Made with Bob
