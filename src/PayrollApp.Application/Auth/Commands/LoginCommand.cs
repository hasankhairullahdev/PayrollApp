using MediatR;
using PayrollApp.Application.Common;

namespace PayrollApp.Application.Auth.Commands;

/// <summary>
/// Command untuk login user
/// </summary>
public record LoginCommand(
    string Email,
    string Password
) : IRequest<Result<LoginResponse>>;

/// <summary>
/// Response dari login command
/// </summary>
public record LoginResponse(
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    string Token
);

// Made with Bob
