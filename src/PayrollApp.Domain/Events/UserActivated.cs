namespace PayrollApp.Domain.Events;

/// <summary>
/// Event raised when a user account is activated
/// </summary>
public record UserActivated(
    Guid UserId,
    DateTime ActivatedAt
);

// Made with Bob
