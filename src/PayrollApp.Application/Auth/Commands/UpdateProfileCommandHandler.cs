using Marten;
using MediatR;
using Microsoft.Extensions.Logging;
using PayrollApp.Application.Common;
using PayrollApp.Domain.Aggregates;

namespace PayrollApp.Application.Auth.Commands;

/// <summary>
/// Handler untuk UpdateProfileCommand
/// </summary>
public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, Result<Unit>>
{
    private readonly IDocumentSession _session;
    private readonly ILogger<UpdateProfileCommandHandler> _logger;

    public UpdateProfileCommandHandler(
        IDocumentSession session,
        ILogger<UpdateProfileCommandHandler> logger)
    {
        _session = session;
        _logger = logger;
    }

    public async Task<Result<Unit>> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Load all events for this user
            var events = await _session.Events.FetchStreamAsync(request.UserId, token: cancellationToken);
            
            if (events == null || !events.Any())
            {
                _logger.LogWarning("User {UserId} not found", request.UserId);
                return Result.Failure<Unit>("User not found");
            }

            // Reconstruct user from events using reflection (private constructor)
            var user = (User)Activator.CreateInstance(typeof(User), true)!;
            foreach (var @event in events)
            {
                ((dynamic)user).Apply((dynamic)@event.Data);
            }

            // Update profile
            user.UpdateProfile(request.FullName);

            // Append new events
            _session.Events.Append(request.UserId, user.GetUncommittedEvents().ToArray());
            await _session.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} profile updated successfully", request.UserId);

            return Result.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile for user {UserId}", request.UserId);
            return Result.Failure<Unit>($"Failed to update profile: {ex.Message}");
        }
    }
}

// Made with Bob