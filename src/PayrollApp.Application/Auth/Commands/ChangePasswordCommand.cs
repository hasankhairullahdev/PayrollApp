using MediatR;
using PayrollApp.Application.Common;

namespace PayrollApp.Application.Auth.Commands;

/// <summary>
/// Command untuk change password user
/// </summary>
public record ChangePasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword
) : IRequest<Result<Unit>>;

// Made with Bob
