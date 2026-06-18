using MediatR;
using PayrollApp.Application.Common;

namespace PayrollApp.Application.Auth.Commands;

/// <summary>
/// Command untuk update profile user
/// </summary>
public record UpdateProfileCommand(
    Guid UserId,
    string FullName
) : IRequest<Result<Unit>>;

// Made with Bob
