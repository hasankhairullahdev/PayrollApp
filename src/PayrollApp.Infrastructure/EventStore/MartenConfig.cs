using Marten;
using Marten.Events.Projections;
using JasperFx.Events.Projections;  // ProjectionLifecycle moved here in Marten 9.x
using Microsoft.Extensions.DependencyInjection;
using PayrollApp.Infrastructure.Projections;

namespace PayrollApp.Infrastructure.EventStore;

public static class MartenConfig
{
    public static void ConfigureMarten(this IServiceCollection services, string connectionString)
    {
        services.AddMarten(opts =>
        {
            // Connection string
            opts.Connection(connectionString);
            
            // Register domain events
            opts.Events.AddEventType<PayrollApp.Domain.Events.PayrollRunCreated>();
            opts.Events.AddEventType<PayrollApp.Domain.Events.PayrollCalculated>();
            opts.Events.AddEventType<PayrollApp.Domain.Events.PayrollReviewStarted>();
            opts.Events.AddEventType<PayrollApp.Domain.Events.PayrollApproved>();
            opts.Events.AddEventType<PayrollApp.Domain.Events.PayrollRejected>();
            opts.Events.AddEventType<PayrollApp.Domain.Events.PayrollLocked>();
            opts.Events.AddEventType<PayrollApp.Domain.Events.PayslipGenerated>();
            opts.Events.AddEventType<PayrollApp.Domain.Events.DisbursementInitiated>();
            opts.Events.AddEventType<PayrollApp.Domain.Events.DisbursementConfirmed>();
            
            // Register projections - Inline for strong consistency
            opts.Projections.Add<PayrollRunSummaryProjection>(ProjectionLifecycle.Inline);
            
            // Database schema name
            opts.Events.DatabaseSchemaName = "payroll_events";
            opts.DatabaseSchemaName = "payroll";
        })
        .UseLightweightSessions();
    }
}

// Made with Bob
