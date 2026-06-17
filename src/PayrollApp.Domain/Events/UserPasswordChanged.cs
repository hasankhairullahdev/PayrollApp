namespace PayrollApp.Domain.Events;

/// <summary>
/// Event raised when a user changes their password
/// </summary>
public record UserPasswordChanged(
    Guid UserId,
    DateTime ChangedAt
);

// Made with Bob
