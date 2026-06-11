using Marten;
using MediatR;
using Microsoft.Extensions.Logging;
using PayrollApp.Application.Common;
using PayrollApp.Infrastructure.ReadModels;

namespace PayrollApp.Application.Payroll.Queries;

/// <summary>
/// Query untuk mendapatkan list PayrollRuns dengan pagination dan filtering.
/// </summary>
public record GetPayrollRunsQuery : IRequest<Result<PayrollRunsResponse>>
{
    public int? Year { get; init; }
    public int? Month { get; init; }
    public string? Status { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

/// <summary>
/// Response untuk GetPayrollRunsQuery
/// </summary>
public record PayrollRunsResponse
{
    public List<PayrollRunSummary> Items { get; init; } = new();
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

/// <summary>
/// Handler untuk GetPayrollRunsQuery
/// </summary>
public class GetPayrollRunsQueryHandler : IRequestHandler<GetPayrollRunsQuery, Result<PayrollRunsResponse>>
{
    private readonly IDocumentStore _documentStore;
    private readonly ILogger<GetPayrollRunsQueryHandler> _logger;

    public GetPayrollRunsQueryHandler(
        IDocumentStore documentStore,
        ILogger<GetPayrollRunsQueryHandler> logger)
    {
        _documentStore = documentStore;
        _logger = logger;
    }

    public async Task<Result<PayrollRunsResponse>> Handle(
        GetPayrollRunsQuery request, 
        CancellationToken cancellationToken)
    {
        try
        {
            await using var session = _documentStore.QuerySession();

            // Build query
            var query = session.Query<PayrollRunSummary>().AsQueryable();

            // Apply filters
            if (request.Year.HasValue)
            {
                query = query.Where(x => x.Year == request.Year.Value);
            }

            if (request.Month.HasValue)
            {
                query = query.Where(x => x.Month == request.Month.Value);
            }

            if (!string.IsNullOrEmpty(request.Status))
            {
                if (Enum.TryParse<Domain.Enums.PayrollStatus>(request.Status, out var status))
                {
                    query = query.Where(x => x.Status == status);
                }
            }

            // Get total count
            var totalCount = await session.Query<PayrollRunSummary>().CountAsync(cancellationToken);

            // Apply pagination and ordering
            var itemsResult = await query
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var response = new PayrollRunsResponse
            {
                Items = itemsResult.ToList(),
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };

            _logger.LogInformation(
                "Retrieved {Count} payroll runs (page {PageNumber}/{TotalPages})",
                itemsResult.Count, request.PageNumber, response.TotalPages);

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payroll runs");
            return Result.Failure<PayrollRunsResponse>($"Failed to retrieve payroll runs: {ex.Message}");
        }
    }
}

// Made with Bob
