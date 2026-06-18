using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PayrollApp.Domain.Aggregates;

namespace PayrollApp.Infrastructure.Security;

/// <summary>
/// Service untuk generate dan validate JWT tokens
/// </summary>
public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly string _secret;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expirationHours;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _secret = configuration["Jwt:Secret"] 
            ?? throw new InvalidOperationException("JWT Secret is not configured");
        _issuer = configuration["Jwt:Issuer"] ?? "PayrollApp";
        _audience = configuration["Jwt:Audience"] ?? "PayrollApp.Client";
        _expirationHours = int.Parse(configuration["Jwt:ExpirationHours"] ?? "8");

        if (_secret.Length < 32)
            throw new InvalidOperationException("JWT Secret must be at least 32 characters long");
    }

    /// <summary>
    /// Generate JWT token untuk user
    /// </summary>
    public string GenerateToken(Guid userId, string email, string fullName, string role)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name cannot be empty", nameof(fullName));
        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("Role cannot be empty", nameof(role));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, fullName),
            new Claim(ClaimTypes.Role, role),
            new Claim("userId", userId.ToString()),
            new Claim("email", email),
            new Claim("role", role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_expirationHours),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Validate JWT token
    /// </summary>
    /// <param name="token">JWT token string</param>
    /// <returns>ClaimsPrincipal jika valid, null jika invalid</returns>
    public ClaimsPrincipal? ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_secret);

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero // No tolerance for expiration
            }, out SecurityToken validatedToken);

            return principal;
        }
        catch
        {
            // Token validation failed
            return null;
        }
    }

    /// <summary>
    /// Extract user ID dari token
    /// </summary>
    /// <param name="token">JWT token string</param>
    /// <returns>User ID jika valid, null jika invalid</returns>
    public Guid? GetUserIdFromToken(string token)
    {
        var principal = ValidateToken(token);
        if (principal == null)
            return null;

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst("userId")?.Value;

        if (string.IsNullOrWhiteSpace(userIdClaim))
            return null;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}

/// <summary>
/// Interface untuk JWT token generator
/// </summary>
public interface IJwtTokenGenerator
{
    string GenerateToken(Guid userId, string email, string fullName, string role);
    ClaimsPrincipal? ValidateToken(string token);
    Guid? GetUserIdFromToken(string token);
}

// Made with Bob
