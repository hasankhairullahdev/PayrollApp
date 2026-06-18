using MediatR;
using Microsoft.AspNetCore.Mvc;
using PayrollApp.Application.Auth.Commands;
using PayrollApp.Application.Auth.Queries;
using PayrollApp.Domain.Enums;

namespace PayrollApp.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication")
            .WithDescription("Authentication and authorization endpoints");

        // POST /api/auth/register
        group.MapPost("/register", async (
            [FromBody] RegisterUserRequest request,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new RegisterUserCommand(
                request.Email,
                request.Password,
                request.FullName,
                request.Role);

            var result = await mediator.Send(command, ct);

            return result.IsSuccess
                ? Results.Ok(new { userId = result.Value, message = "User registered successfully" })
                : Results.BadRequest(new { error = result.Error });
        })
        .AllowAnonymous()
        .WithName("RegisterUser")
        .WithSummary("Register a new user")
        .Produces<RegisterUserResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        // POST /api/auth/login
        group.MapPost("/login", async (
            [FromBody] LoginRequest request,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new LoginCommand(request.Email, request.Password);
            var result = await mediator.Send(command, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Json(new { error = result.Error }, statusCode: StatusCodes.Status401Unauthorized);
        })
        .AllowAnonymous()
        .WithName("Login")
        .WithSummary("Login with email and password")
        .Produces<LoginResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized);

        // GET /api/auth/me
        group.MapGet("/me", async (
            HttpContext context,
            IMediator mediator,
            CancellationToken ct) =>
        {
            // Get user ID from JWT claims
            var userIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Results.Unauthorized();
            }

            var query = new GetCurrentUserQuery(userId);
            var result = await mediator.Send(query, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error });
        })
        .RequireAuthorization()
        .WithName("GetCurrentUser")
        .WithSummary("Get current authenticated user info")
        .Produces<CurrentUserResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status404NotFound);

        // PUT /api/auth/profile
        group.MapPut("/profile", async (
            HttpContext context,
            [FromBody] UpdateProfileRequest request,
            IMediator mediator,
            CancellationToken ct) =>
        {
            // Get user ID from JWT claims
            var userIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Results.Unauthorized();
            }

            var command = new UpdateProfileCommand(userId, request.FullName);
            var result = await mediator.Send(command, ct);

            return result.IsSuccess
                ? Results.Ok(new { message = "Profile updated successfully" })
                : Results.BadRequest(new { error = result.Error });
        })
        .RequireAuthorization()
        .WithName("UpdateProfile")
        .WithSummary("Update user profile")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized);

        // POST /api/auth/change-password
        group.MapPost("/change-password", async (
            HttpContext context,
            [FromBody] ChangePasswordRequest request,
            IMediator mediator,
            CancellationToken ct) =>
        {
            // Get user ID from JWT claims
            var userIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Results.Unauthorized();
            }

            var command = new ChangePasswordCommand(userId, request.CurrentPassword, request.NewPassword);
            var result = await mediator.Send(command, ct);

            return result.IsSuccess
                ? Results.Ok(new { message = "Password changed successfully" })
                : Results.BadRequest(new { error = result.Error });
        })
        .RequireAuthorization()
        .WithName("ChangePassword")
        .WithSummary("Change user password")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized);

        return app;
    }
}

// Request/Response DTOs
public record RegisterUserRequest(
    string Email,
    string Password,
    string FullName,
    UserRole Role);

public record RegisterUserResponse(
    Guid UserId,
    string Message);

public record LoginRequest(
    string Email,
    string Password);

public record CurrentUserResponse(
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastLoginAt);

public record UpdateProfileRequest(string FullName);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword);

public record ErrorResponse(string Error);

// Made with Bob
