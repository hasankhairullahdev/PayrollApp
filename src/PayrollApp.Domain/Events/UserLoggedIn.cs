namespace PayrollApp.Domain.Events;

/// <summary>
/// Event raised when a user successfully logs in
/// </summary>
public record UserLoggedIn(
    Guid UserId,
    DateTime LoginAt
);

// Made with Bob
