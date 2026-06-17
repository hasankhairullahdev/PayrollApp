namespace PayrollApp.Domain.Events;

/// <summary>
/// Event raised when a user account is deactivated
/// </summary>
public record UserDeactivated(
    Guid UserId,
    DateTime DeactivatedAt
);

// Made with Bob
