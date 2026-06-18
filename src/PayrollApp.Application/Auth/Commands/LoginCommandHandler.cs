using Marten;
using MediatR;
using Microsoft.Extensions.Logging;
using PayrollApp.Application.Common;
using PayrollApp.Domain.Aggregates;
using PayrollApp.Domain.Enums;
using PayrollApp.Domain.Events;
using PayrollApp.Infrastructure.ReadModels;
using PayrollApp.Infrastructure.Security;

namespace PayrollApp.Application.Auth.Commands;

/// <summary>
/// Handler untuk LoginCommand
/// </summary>
public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    private readonly IDocumentSession _session;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IDocumentSession session,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator tokenGenerator,
        ILogger<LoginCommandHandler> logger)
    {
        _session = session;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _logger = logger;
    }

    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Find user by email from read model
            var userReadModel = await _session.Query<UserReadModel>()
                .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), cancellationToken);

            if (userReadModel == null)
            {
                _logger.LogWarning("Login failed: User with email {Email} not found", request.Email);
                return Result.Failure<LoginResponse>("Invalid email or password");
            }

            // Verify password
            if (!_passwordHasher.VerifyPassword(request.Password, userReadModel.PasswordHash))
            {
                _logger.LogWarning("Login failed: Invalid password for email {Email}", request.Email);
                return Result.Failure<LoginResponse>("Invalid email or password");
            }

            // Check if user is active
            if (!userReadModel.IsActive)
            {
                _logger.LogWarning("Login failed: User {UserId} is deactivated", userReadModel.Id);
                return Result.Failure<LoginResponse>("User account is deactivated");
            }

            // Generate JWT token using read model data
            var token = _tokenGenerator.GenerateToken(
                userReadModel.Id,
                userReadModel.Email,
                userReadModel.FullName,
                userReadModel.Role
            );

            // Record login event directly without loading aggregate
            var loginEvent = new UserLoggedIn(userReadModel.Id, DateTime.UtcNow);
            _session.Events.Append(userReadModel.Id, loginEvent);
            await _session.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} logged in successfully", userReadModel.Id);

            var response = new LoginResponse(
                userReadModel.Id,
                userReadModel.Email,
                userReadModel.FullName,
                userReadModel.Role,
                token
            );

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for email {Email}", request.Email);
            return Result.Failure<LoginResponse>($"Login failed: {ex.Message}");
        }
    }
}

// Made with Bob
