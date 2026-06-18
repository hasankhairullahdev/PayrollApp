using Microsoft.AspNetCore.Authorization;

namespace PayrollApp.Infrastructure.Security;

/// <summary>
/// Centralized authorization policy definitions
/// </summary>
public static class AuthorizationPolicies
{
    // Policy names
    public const string RequireAdminRole = "RequireAdminRole";
    public const string RequireHRRole = "RequireHRRole";
    public const string RequireFinanceRole = "RequireFinanceRole";
    public const string RequireEmployeeRole = "RequireEmployeeRole";
    
    // Combined policies
    public const string RequireHROrAdmin = "RequireHROrAdmin";
    public const string RequireFinanceOrAdmin = "RequireFinanceOrAdmin";
    public const string RequireHROrFinanceOrAdmin = "RequireHROrFinanceOrAdmin";
    
    /// <summary>
    /// Configure all authorization policies
    /// </summary>
    public static void AddAuthorizationPolicies(this AuthorizationOptions options)
    {
        // Single role policies
        options.AddPolicy(RequireAdminRole, policy =>
            policy.RequireRole("Admin"));
        
        options.AddPolicy(RequireHRRole, policy =>
            policy.RequireRole("HR"));
        
        options.AddPolicy(RequireFinanceRole, policy =>
            policy.RequireRole("Finance"));
        
        options.AddPolicy(RequireEmployeeRole, policy =>
            policy.RequireRole("Employee"));
        
        // Combined role policies
        options.AddPolicy(RequireHROrAdmin, policy =>
            policy.RequireRole("HR", "Admin"));
        
        options.AddPolicy(RequireFinanceOrAdmin, policy =>
            policy.RequireRole("Finance", "Admin"));
        
        options.AddPolicy(RequireHROrFinanceOrAdmin, policy =>
            policy.RequireRole("HR", "Finance", "Admin"));
    }
}

// Made with Bob
