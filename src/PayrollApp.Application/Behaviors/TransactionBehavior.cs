using Marten;
using MediatR;
using Microsoft.Extensions.Logging;

namespace PayrollApp.Application.Behaviors;

/// <summary>
/// Pipeline behavior untuk wrapping command dalam transaction.
/// Hanya untuk commands (yang mengubah state), tidak untuk queries.
/// </summary>
public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IDocumentStore _documentStore;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public TransactionBehavior(
        IDocumentStore documentStore,
        ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _documentStore = documentStore;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        
        // Skip transaction untuk queries (by convention, queries tidak mengubah state)
        if (requestName.EndsWith("Query"))
        {
            return await next();
        }

        // Untuk commands, wrap dalam transaction
        _logger.LogDebug("Starting transaction for {RequestName}", requestName);
        
        await using var session = _documentStore.LightweightSession();
        
        try
        {
            var response = await next();
            
            // Commit transaction
            await session.SaveChangesAsync(cancellationToken);
            
            _logger.LogDebug("Transaction committed for {RequestName}", requestName);
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transaction failed for {RequestName}", requestName);
            throw;
        }
    }
}

// Made with Bob
