using MediatR;
using Microsoft.AspNetCore.Mvc;
using PayrollApp.Application.Auth.Commands;
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
                : Results.Unauthorized();
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

            // TODO: Create GetCurrentUserQuery to fetch user details
            // For now, return basic info from claims
            var email = context.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var role = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            var name = context.User.Identity?.Name;

            return Results.Ok(new
            {
                userId,
                email,
                role,
                name
            });
        })
        .RequireAuthorization()
        .WithName("GetCurrentUser")
        .WithSummary("Get current authenticated user info")
        .Produces<CurrentUserResponse>(StatusCodes.Status200OK)
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
    string Role,
    string? Name);

public record ErrorResponse(string Error);

// Made with Bob
