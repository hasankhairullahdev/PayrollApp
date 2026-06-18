using FluentValidation;
using Marten;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using PayrollApp.Api;
using PayrollApp.Api.Endpoints;
using PayrollApp.Application.Behaviors;
using PayrollApp.Engine;
using PayrollApp.Infrastructure.EventStore;
using PayrollApp.Infrastructure.Jobs;
using PayrollApp.Infrastructure.Repositories;
using PayrollApp.Infrastructure.Security;

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

// Add Security Services
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

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

// Add Authentication & Authorization
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero // Remove default 5 minute clock skew
        };
        
        // Skip authentication for endpoints marked with [AllowAnonymous]
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // Allow anonymous endpoints to proceed without token
                var endpoint = context.HttpContext.GetEndpoint();
                if (endpoint?.Metadata?.GetMetadata<Microsoft.AspNetCore.Authorization.IAllowAnonymous>() != null)
                {
                    context.NoResult();
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddAuthorizationPolicies();
});

var app = builder.Build();

// Apply Marten schema and seed initial data
using (var scope = app.Services.CreateScope())
{
    var documentStore = scope.ServiceProvider.GetRequiredService<IDocumentStore>();
    
    // Apply database schema
    Console.WriteLine("Applying Marten database schema...");
    await documentStore.Storage.ApplyAllConfiguredChangesToDatabaseAsync();
    Console.WriteLine("✓ Database schema applied successfully");
    
    // Seed data
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    await SeedData.SeedUsersAsync(documentStore, passwordHasher);
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

// Use Authentication & Authorization (MUST be before MapEndpoints)
app.UseAuthentication();
app.UseAuthorization();

// Map endpoints
app.MapAuthEndpoints();
app.MapPayrollEndpoints();
app.MapReportEndpoints();
app.MapEmployeeEndpoints();
app.MapEventEndpoints();

// Use Hangfire Dashboard (after endpoints)
app.UseHangfireDashboardWithAuth();

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
