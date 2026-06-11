using FluentValidation;
using MediatR;
using PayrollApp.Application.Common;

namespace PayrollApp.Application.Behaviors;

/// <summary>
/// Pipeline behavior untuk validasi request menggunakan FluentValidation.
/// Dijalankan sebelum handler untuk memastikan request valid.
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Skip validation jika tidak ada validator
        if (!_validators.Any())
        {
            return await next();
        }

        // Jalankan semua validator
        var context = new ValidationContext<TRequest>(request);
        
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken))
        );

        // Kumpulkan semua error
        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        // Jika ada error, return failure result
        if (failures.Any())
        {
            var errorMessages = string.Join("; ", failures.Select(f => f.ErrorMessage));
            
            // Create failure result using reflection
            // Karena TResponse bisa Result atau Result<T>
            var resultType = typeof(TResponse);
            
            if (resultType.IsGenericType)
            {
                // Result<T>
                var valueType = resultType.GetGenericArguments()[0];
                var failureMethod = typeof(Result)
                    .GetMethod(nameof(Result.Failure))!
                    .MakeGenericMethod(valueType);
                
                return (TResponse)failureMethod.Invoke(null, new object[] { errorMessages })!;
            }
            else
            {
                // Result
                return (TResponse)(object)Result.Failure(errorMessages);
            }
        }

        // Lanjutkan ke handler berikutnya
        return await next();
    }
}

// Made with Bob
