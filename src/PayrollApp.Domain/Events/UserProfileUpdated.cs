namespace PayrollApp.Domain.Events;

/// <summary>
/// Event raised when a user updates their profile information
/// </summary>
public record UserProfileUpdated(
    Guid UserId,
    string FullName,
    DateTime UpdatedAt
);

// Made with Bob
