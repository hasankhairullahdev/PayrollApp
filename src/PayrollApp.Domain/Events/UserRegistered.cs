using PayrollApp.Domain.Enums;

namespace PayrollApp.Domain.Events;

/// <summary>
/// Event raised when a new user is registered in the system
/// </summary>
public record UserRegistered(
    Guid UserId,
    string Email,
    string PasswordHash,
    string FullName,
    UserRole Role,
    DateTime RegisteredAt
);

// Made with Bob
