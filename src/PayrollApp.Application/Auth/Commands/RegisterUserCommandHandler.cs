using Marten;
using MediatR;
using Microsoft.Extensions.Logging;
using PayrollApp.Application.Common;
using PayrollApp.Domain.Aggregates;
using PayrollApp.Infrastructure.Security;

namespace PayrollApp.Application.Auth.Commands;

/// <summary>
/// Handler untuk RegisterUserCommand
/// </summary>
public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<Guid>>
{
    private readonly IDocumentSession _session;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<RegisterUserCommandHandler> _logger;

    public RegisterUserCommandHandler(
        IDocumentSession session,
        IPasswordHasher passwordHasher,
        ILogger<RegisterUserCommandHandler> logger)
    {
        _session = session;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check if email already exists
            var existingUser = await _session.Query<User>()
                .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), cancellationToken);

            if (existingUser != null)
            {
                _logger.LogWarning("Registration failed: Email {Email} already exists", request.Email);
                return Result.Failure<Guid>("Email already registered");
            }

            // Hash password
            var passwordHash = _passwordHasher.HashPassword(request.Password);

            // Create user aggregate
            var user = User.Register(
                request.Email,
                passwordHash,
                request.FullName,
                request.Role
            );

            // Save to Marten
            _session.Events.StartStream<User>(user.Id, user.GetUncommittedEvents().ToArray());
            await _session.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} registered successfully with email {Email}", 
                user.Id, request.Email);

            return Result<Guid>.Success(user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user with email {Email}", request.Email);
            return Result.Failure<Guid>($"Failed to register user: {ex.Message}");
        }
    }
}

// Made with Bob
