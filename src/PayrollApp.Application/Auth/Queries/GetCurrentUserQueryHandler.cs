using Marten;
using MediatR;
using Microsoft.Extensions.Logging;
using PayrollApp.Application.Common;
using PayrollApp.Infrastructure.ReadModels;

namespace PayrollApp.Application.Auth.Queries;

/// <summary>
/// Handler untuk GetCurrentUserQuery
/// </summary>
public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, Result<CurrentUserResponse>>
{
    private readonly IDocumentSession _session;
    private readonly ILogger<GetCurrentUserQueryHandler> _logger;

    public GetCurrentUserQueryHandler(
        IDocumentSession session,
        ILogger<GetCurrentUserQueryHandler> logger)
    {
        _session = session;
        _logger = logger;
    }

    public async Task<Result<CurrentUserResponse>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _session.LoadAsync<UserReadModel>(request.UserId, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", request.UserId);
                return Result.Failure<CurrentUserResponse>("User not found");
            }

            var response = new CurrentUserResponse(
                user.Id,
                user.Email,
                user.FullName,
                user.Role,
                user.IsActive,
                user.CreatedAt,
                user.LastLoginAt
            );

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user {UserId}", request.UserId);
            return Result.Failure<CurrentUserResponse>($"Failed to get user: {ex.Message}");
        }
    }
}

// Made with Bob