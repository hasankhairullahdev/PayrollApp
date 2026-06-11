using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace PayrollApp.Infrastructure.Jobs;

public static class HangfireConfig
{
    public static void ConfigureHangfire(this IServiceCollection services, string connectionString)
    {
        // Add Hangfire services dengan PostgreSQL storage
        services.AddHangfire(config =>
        {
            config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(options =>
                {
                    options.UseNpgsqlConnection(connectionString);
                })
                .WithJobExpirationTimeout(TimeSpan.FromDays(7)); // Keep job history for 7 days
        });
        
        // Add Hangfire server
        services.AddHangfireServer(options =>
        {
            options.WorkerCount = 5; // Number of concurrent jobs
            options.Queues = new[] { "default", "payroll", "reports" }; // Queue priorities
            options.ServerName = $"PayrollApp-{Environment.MachineName}";
        });
    }
    
    public static void UseHangfireDashboardWithAuth(this IApplicationBuilder app)
    {
        // Configure Hangfire dashboard dengan basic authentication
        var dashboardOptions = new DashboardOptions
        {
            Authorization = new[] { new HangfireAuthorizationFilter() },
            DashboardTitle = "Payroll App - Background Jobs",
            StatsPollingInterval = 2000, // Update stats every 2 seconds
            DisplayStorageConnectionString = false // Don't show connection string in dashboard
        };
        
        app.UseHangfireDashboard("/hangfire", dashboardOptions);
    }
}

/// <summary>
/// Basic authorization filter untuk Hangfire dashboard.
/// Di production, ganti dengan proper authentication (JWT, OAuth, dll).
/// </summary>
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // Di development, allow semua akses
        // Di production, check user authentication/authorization
        var httpContext = context.GetHttpContext();
        
        // TODO: Implement proper authorization
        // For now, allow all in development
        return true;
        
        // Production example:
        // return httpContext.User.Identity?.IsAuthenticated == true 
        //     && httpContext.User.IsInRole("Admin");
    }
}

// Made with Bob
