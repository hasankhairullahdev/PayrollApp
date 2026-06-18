using Marten.Events.Aggregation;
using PayrollApp.Domain.Events;
using PayrollApp.Infrastructure.ReadModels;

namespace PayrollApp.Infrastructure.Projections;

/// <summary>
/// Projection for User read model - builds queryable user data from events
/// </summary>
public partial class UserReadModelProjection : SingleStreamProjection<UserReadModel, Guid>
{
    public UserReadModel Create(UserRegistered @event)
    {
        return new UserReadModel
        {
            Id = @event.UserId,
            Email = @event.Email,
            PasswordHash = @event.PasswordHash,
            FullName = @event.FullName,
            Role = @event.Role.ToString(),
            IsActive = true,
            CreatedAt = @event.RegisteredAt
        };
    }

    public void Apply(UserLoggedIn @event, UserReadModel user)
    {
        user.LastLoginAt = @event.LoginAt;
    }

    public void Apply(UserPasswordChanged @event, UserReadModel user)
    {
        // Password hash updated separately in handler
    }

    public void Apply(UserProfileUpdated @event, UserReadModel user)
    {
        user.FullName = @event.FullName;
    }

    public void Apply(UserDeactivated @event, UserReadModel user)
    {
        user.IsActive = false;
    }

    public void Apply(UserActivated @event, UserReadModel user)
    {
        user.IsActive = true;
    }
}

// Made with Bob