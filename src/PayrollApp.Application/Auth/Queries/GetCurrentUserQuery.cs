using MediatR;
using PayrollApp.Application.Common;

namespace PayrollApp.Application.Auth.Queries;

/// <summary>
/// Query untuk mendapatkan informasi user yang sedang login
/// </summary>
public record GetCurrentUserQuery(Guid UserId) : IRequest<Result<CurrentUserResponse>>;

/// <summary>
/// Response untuk current user info
/// </summary>
public record CurrentUserResponse(
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastLoginAt
);

// Made with Bob
