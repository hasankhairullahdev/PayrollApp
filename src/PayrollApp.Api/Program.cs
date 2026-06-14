using FluentValidation;
using Marten;
using PayrollApp.Api;
using PayrollApp.Api.Endpoints;
using PayrollApp.Application.Behaviors;
using PayrollApp.Engine;
using PayrollApp.Infrastructure.EventStore;
using PayrollApp.Infrastructure.Jobs;
using PayrollApp.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Configure JSON options to serialize enums as strings
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Payroll API",
        Version = "v1",
        Description = "Enterprise Payroll Application API"
    });
});

// Get connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found");

// Configure Marten (Event Store + Read Models)
builder.Services.ConfigureMarten(connectionString);

// Configure Hangfire (Background Jobs)
builder.Services.ConfigureHangfire(connectionString);

// Add MediatR
builder.Services.AddMediatR(config =>
{
    // Register handlers from Application assembly
    config.RegisterServicesFromAssembly(typeof(PayrollApp.Application.Common.Result).Assembly);
    
    // Add pipeline behaviors (order matters!)
    config.AddOpenBehavior(typeof(ValidationBehavior<,>));
    config.AddOpenBehavior(typeof(LoggingBehavior<,>));
    config.AddOpenBehavior(typeof(TransactionBehavior<,>));
});

// Add FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(PayrollApp.Application.Common.Result).Assembly);

// Note: Calculation Engine classes (PPh21Calculator, BPJSCalculator, etc.) are static
// No need to register them in DI container

// Add Repositories
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();

// Add Background Jobs
builder.Services.AddScoped<PayrollCalculationJob>();
builder.Services.AddScoped<PayslipGenerationJob>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000") // Next.js default port
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Seed initial data
using (var scope = app.Services.CreateScope())
{
    var documentStore = scope.ServiceProvider.GetRequiredService<IDocumentStore>();
    await SeedData.SeedEmployeesAsync(documentStore);
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Use CORS
app.UseCors();

// Use Hangfire Dashboard
app.UseHangfireDashboardWithAuth();

// Map endpoints
app.MapPayrollEndpoints();
app.MapReportEndpoints();
app.MapEmployeeEndpoints();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    Status = "Healthy",
    Timestamp = DateTime.UtcNow,
    Environment = app.Environment.EnvironmentName
}))
.WithName("HealthCheck")
.WithTags("Health");

app.Run();

// Made with Bob
