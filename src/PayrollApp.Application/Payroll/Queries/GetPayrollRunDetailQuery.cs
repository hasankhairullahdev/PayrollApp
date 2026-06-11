using Marten;
using MediatR;
using Microsoft.Extensions.Logging;
using PayrollApp.Application.Common;
using PayrollApp.Infrastructure.ReadModels;

namespace PayrollApp.Application.Payroll.Queries;

/// <summary>
/// Query untuk mendapatkan detail PayrollRun beserta line items.
/// </summary>
public record GetPayrollRunDetailQuery : IRequest<Result<PayrollRunDetailResponse>>
{
    public Guid PayrollRunId { get; init; }
}

/// <summary>
/// Response untuk GetPayrollRunDetailQuery
/// </summary>
public record PayrollRunDetailResponse
{
    public PayrollRunSummary Summary { get; init; } = null!;
    public List<PayrollLineItem> LineItems { get; init; } = new();
}

/// <summary>
/// Handler untuk GetPayrollRunDetailQuery
/// </summary>
public class GetPayrollRunDetailQueryHandler : IRequestHandler<GetPayrollRunDetailQuery, Result<PayrollRunDetailResponse>>
{
    private readonly IDocumentStore _documentStore;
    private readonly ILogger<GetPayrollRunDetailQueryHandler> _logger;

    public GetPayrollRunDetailQueryHandler(
        IDocumentStore documentStore,
        ILogger<GetPayrollRunDetailQueryHandler> logger)
    {
        _documentStore = documentStore;
        _logger = logger;
    }

    public async Task<Result<PayrollRunDetailResponse>> Handle(
        GetPayrollRunDetailQuery request, 
        CancellationToken cancellationToken)
    {
        try
        {
            await using var session = _documentStore.QuerySession();

            // Get summary
            var summary = await session.LoadAsync<PayrollRunSummary>(
                request.PayrollRunId, 
                cancellationToken);

            if (summary == null)
            {
                return Result.Failure<PayrollRunDetailResponse>(
                    $"PayrollRun {request.PayrollRunId} not found");
            }

            // Get line items
            var lineItems = await session.Query<PayrollLineItem>()
                .Where(x => x.PayrollRunId == request.PayrollRunId)
                .OrderBy(x => x.EmployeeName)
                .ToListAsync(cancellationToken);

            var response = new PayrollRunDetailResponse
            {
                Summary = summary,
                LineItems = lineItems
            };

            _logger.LogInformation(
                "Retrieved PayrollRun {PayrollRunId} detail with {LineItemCount} line items",
                request.PayrollRunId, lineItems.Count);

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving PayrollRun {PayrollRunId} detail", 
                request.PayrollRunId);
            
            return Result.Failure<PayrollRunDetailResponse>(
                $"Failed to retrieve payroll run detail: {ex.Message}");
        }
    }
}

// Made with Bob
