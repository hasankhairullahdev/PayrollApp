using MediatR;
using PayrollApp.Application.Common;
using PayrollApp.Domain.Enums;

namespace PayrollApp.Application.Auth.Commands;

/// <summary>
/// Command untuk register user baru
/// </summary>
public record RegisterUserCommand(
    string Email,
    string Password,
    string FullName,
    UserRole Role
) : IRequest<Result<Guid>>;

// Made with Bob
