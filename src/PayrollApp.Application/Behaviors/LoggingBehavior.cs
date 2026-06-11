using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace PayrollApp.Application.Behaviors;

/// <summary>
/// Pipeline behavior untuk logging request/response dan performance monitoring.
/// </summary>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        
        _logger.LogInformation("Handling {RequestName}", requestName);
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var response = await next();
            
            stopwatch.Stop();
            
            _logger.LogInformation(
                "Handled {RequestName} in {ElapsedMilliseconds}ms",
                requestName,
                stopwatch.ElapsedMilliseconds
            );
            
            // Log warning jika request lambat (> 3 detik)
            if (stopwatch.ElapsedMilliseconds > 3000)
            {
                _logger.LogWarning(
                    "Long running request: {RequestName} took {ElapsedMilliseconds}ms",
                    requestName,
                    stopwatch.ElapsedMilliseconds
                );
            }
            
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(
                ex,
                "Error handling {RequestName} after {ElapsedMilliseconds}ms",
                requestName,
                stopwatch.ElapsedMilliseconds
            );
            
            throw;
        }
    }
}

// Made with Bob
