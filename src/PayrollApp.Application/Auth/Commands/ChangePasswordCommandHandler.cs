using Marten;
using MediatR;
using Microsoft.Extensions.Logging;
using PayrollApp.Application.Common;
using PayrollApp.Domain.Aggregates;
using PayrollApp.Infrastructure.ReadModels;
using PayrollApp.Infrastructure.Security;

namespace PayrollApp.Application.Auth.Commands;

/// <summary>
/// Handler untuk ChangePasswordCommand
/// </summary>
public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result<Unit>>
{
    private readonly IDocumentSession _session;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<ChangePasswordCommandHandler> _logger;

    public ChangePasswordCommandHandler(
        IDocumentSession session,
        IPasswordHasher passwordHasher,
        ILogger<ChangePasswordCommandHandler> logger)
    {
        _session = session;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<Result<Unit>> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Load user read model untuk verify current password
            var userReadModel = await _session.LoadAsync<UserReadModel>(request.UserId, cancellationToken);

            if (userReadModel == null)
            {
                _logger.LogWarning("User {UserId} not found", request.UserId);
                return Result.Failure<Unit>("User not found");
            }

            // Verify current password
            if (!_passwordHasher.VerifyPassword(request.CurrentPassword, userReadModel.PasswordHash))
            {
                _logger.LogWarning("Invalid current password for user {UserId}", request.UserId);
                return Result.Failure<Unit>("Current password is incorrect");
            }

            // Load all events for this user
            var events = await _session.Events.FetchStreamAsync(request.UserId, token: cancellationToken);
            
            if (events == null || !events.Any())
            {
                _logger.LogWarning("User aggregate {UserId} not found", request.UserId);
                return Result.Failure<Unit>("User not found");
            }

            // Reconstruct user from events using reflection (private constructor)
            var user = (User)Activator.CreateInstance(typeof(User), true)!;
            foreach (var @event in events)
            {
                ((dynamic)user).Apply((dynamic)@event.Data);
            }

            // Hash new password
            var newPasswordHash = _passwordHasher.HashPassword(request.NewPassword);

            // Change password
            user.ChangePassword(newPasswordHash);

            // Append new events
            _session.Events.Append(request.UserId, user.GetUncommittedEvents().ToArray());
            await _session.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Password changed successfully for user {UserId}", request.UserId);

            return Result.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", request.UserId);
            return Result.Failure<Unit>($"Failed to change password: {ex.Message}");
        }
    }
}

// Made with Bob