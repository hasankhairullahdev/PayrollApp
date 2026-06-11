using Marten;
using Marten.Events.Projections;
using PayrollApp.Domain.Aggregates;
using PayrollApp.Infrastructure.Projections;
using Weasel.Core;

namespace PayrollApp.Infrastructure.EventStore;

public static class MartenConfig
{
    public static void ConfigureMarten(this IServiceCollection services, string connectionString)
    {
        services.AddMarten(options =>
        {
            // Connection string
            options.Connection(connectionString);
            
            // Auto create/update schema
            options.AutoCreateSchemaObjects = AutoCreate.All;
            
            // Event store configuration
            options.Events.StreamIdentity = StreamIdentity.AsGuid;
            
            // Register aggregates for event sourcing
            options.Events.AddEventType<PayrollApp.Domain.Events.PayrollRunCreated>();
            options.Events.AddEventType<PayrollApp.Domain.Events.PayrollCalculated>();
            options.Events.AddEventType<PayrollApp.Domain.Events.PayrollReviewStarted>();
            options.Events.AddEventType<PayrollApp.Domain.Events.PayrollApproved>();
            options.Events.AddEventType<PayrollApp.Domain.Events.PayrollRejected>();
            options.Events.AddEventType<PayrollApp.Domain.Events.PayrollLocked>();
            options.Events.AddEventType<PayrollApp.Domain.Events.PayslipGenerated>();
            options.Events.AddEventType<PayrollApp.Domain.Events.DisbursementInitiated>();
            options.Events.AddEventType<PayrollApp.Domain.Events.DisbursementConfirmed>();
            
            // Register projections
            options.Projections.Add<PayrollRunSummaryProjection>(ProjectionLifecycle.Inline);
            
            // Use optimistic concurrency by default
            options.UseDefaultSerialization(
                EnumStorage.AsString,
                nonPublicMembersStorage: NonPublicMembersStorage.All
            );
            
            // Database schema name
            options.Events.DatabaseSchemaName = "payroll_events";
            options.DatabaseSchemaName = "payroll";
        })
        .UseLightweightSessions() // For better performance
        .OptimizeArtifactWorkflow(); // Optimize for development
    }
}

// Made with Bob
