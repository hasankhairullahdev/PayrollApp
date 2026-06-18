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
            
            // Configure Employee document to properly handle nested collections
            opts.Schema.For<PayrollApp.Domain.Aggregates.Employee>()
                .UseOptimisticConcurrency(true);
            
            // Register document types
            opts.RegisterDocumentType<PayrollApp.Domain.Aggregates.Employee>();
            opts.RegisterDocumentType<PayrollApp.Infrastructure.ReadModels.PayrollRunSummary>();
            opts.RegisterDocumentType<PayrollApp.Infrastructure.ReadModels.PayrollLineItem>();
            opts.RegisterDocumentType<PayrollApp.Infrastructure.ReadModels.UserReadModel>();
            
            // Register Payroll domain events
            opts.Events.AddEventType<PayrollApp.Domain.Events.PayrollRunCreated>();
            opts.Events.AddEventType<PayrollApp.Domain.Events.PayrollCalculationStarted>();
            opts.Events.AddEventType<PayrollApp.Domain.Events.PayrollCalculated>();
            opts.Events.AddEventType<PayrollApp.Domain.Events.PayrollReviewStarted>();
            opts.Events.AddEventType<PayrollApp.Domain.Events.PayrollApproved>();
            opts.Events.AddEventType<PayrollApp.Domain.Events.PayrollRejected>();
            opts.Events.AddEventType<PayrollApp.Domain.Events.PayrollLocked>();
            opts.Events.AddEventType<PayrollApp.Domain.Events.PayslipGenerated>();
            opts.Events.AddEventType<PayrollApp.Domain.Events.DisbursementInitiated>();
            opts.Events.AddEventType<PayrollApp.Domain.Events.DisbursementConfirmed>();
            
            // Register User domain events
            opts.Events.AddEventType<PayrollApp.Domain.Events.UserRegistered>();
            opts.Events.AddEventType<PayrollApp.Domain.Events.UserLoggedIn>();
            opts.Events.AddEventType<PayrollApp.Domain.Events.UserPasswordChanged>();
            opts.Events.AddEventType<PayrollApp.Domain.Events.UserProfileUpdated>();
            opts.Events.AddEventType<PayrollApp.Domain.Events.UserDeactivated>();
            opts.Events.AddEventType<PayrollApp.Domain.Events.UserActivated>();
            
            // Register projections - Inline for strong consistency
            opts.Projections.Add<PayrollRunSummaryProjection>(ProjectionLifecycle.Inline);
            opts.Projections.Add<PayrollLineItemProjection>(ProjectionLifecycle.Inline);
            opts.Projections.Add<UserReadModelProjection>(ProjectionLifecycle.Inline);
            
            // Use default schema names (public) for simplicity
            // opts.Events.DatabaseSchemaName = "payroll_events";
            // opts.DatabaseSchemaName = "payroll";
        })
        .UseLightweightSessions();
    }
}

// Made with Bob
