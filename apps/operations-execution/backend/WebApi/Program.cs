using DataModel;
using DataModel.Repository;
using Domain.IRepository;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using WebApi.Middleware;
using WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger/OpenAPI with Dev Authentication
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "OEM API - Operations & Execution Management",
        Version = "v1",
        Description = "REST API for managing port operations execution data, including operation plans, vessel visit executions, incidents, and complementary tasks.",
        Contact = new OpenApiContact
        {
            Name = "LEI-ISEP Team",
            Email = "team@example.com"
        }
    });

    // Configure Dev Authentication in Swagger (US 4.1.1)
    c.AddSecurityDefinition("X-User-Email", new OpenApiSecurityScheme
    {
        Description = "User email for development authentication (e.g., admin@example.com for Administrator, operator@example.com for LogisticsOperator)",
        Name = "X-User-Email",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "X-User-Email"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "X-User-Email"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configure Database - MySQL on Azure
var connectionString = builder.Configuration.GetConnectionString("OEMDatabase");

builder.Services.AddDbContext<OEMDbContext>(options =>
{
    var isIntegration = Environment.GetEnvironmentVariable("ASPNETCORE_INTEGRATION_TEST") == "true";
    if (isIntegration)
    {
        // In integration tests we override to use in-memory SQLite
        options.UseSqlite(connectionString ?? "DataSource=:memory:");
    }
    else
    {
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
    }
});

// Register Repositories
builder.Services.AddScoped<IOperationPlanRepository, OperationPlanRepository>();
builder.Services.AddScoped<IVesselVisitExecutionRepository, VesselVisitExecutionRepository>();
builder.Services.AddScoped<IExecutedOperationRepository, ExecutedOperationRepository>();
builder.Services.AddScoped<IIncidentTypeRepository, IncidentTypeRepository>();
builder.Services.AddScoped<IIncidentRepository, IncidentRepository>();
builder.Services.AddScoped<IComplementaryTaskCategoryRepository, ComplementaryTaskCategoryRepository>();
builder.Services.AddScoped<IComplementaryTaskRepository, ComplementaryTaskRepository>();

// Register Authorization Service (US 4.1.1)
builder.Services.AddScoped<RoleAssignmentService>();

// Register Application Services
builder.Services.AddScoped<Application.Services.OperationPlanService>();
builder.Services.AddScoped<Application.Services.IncidentTypeService>();
builder.Services.AddScoped<Application.Services.IncidentService>();
builder.Services.AddScoped<Application.Services.VesselVisitExecutionService>();
builder.Services.AddScoped<Application.Services.ComplementaryTaskService>();
builder.Services.AddScoped<Application.Services.ExecutedOperationService>();

// Register HttpClient for Scheduling Service (US 4.1.2)
builder.Services.AddHttpClient<Application.Services.SchedulingServiceClient>(client =>
{
    // Default configuration - URL will be read from appsettings
    client.Timeout = TimeSpan.FromSeconds(60);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    // Accept self-signed SSL certificates for development
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
});

// Register HttpClient for Port Logistics validation (US 4.1.2 - VVN validation)
builder.Services.AddHttpClient<Application.Services.PortLogisticsClient>(client =>
{
    var portLogisticsUrl = builder.Configuration["PortLogistics:BaseUrl"] ?? "http://localhost:7095";
    client.BaseAddress = new Uri(portLogisticsUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    // Use a valid Port Logistics user email (Port Authority Officer has access to all VVNs)
    client.DefaultRequestHeaders.Add("X-User-Email", "admin@example.com");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    // Accept self-signed SSL certificates for development
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
});

// Configure CORS
builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration.GetSection("CORS:AllowedOrigins").Get<string[]>() 
        ?? new[] { "http://localhost:4200", "https://localhost:4200" };
    
    options.AddPolicy("AllowAll",
        policyBuilder => policyBuilder
            .WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .WithExposedHeaders("Content-Disposition", "X-User-Email", "X-User-Role"));
});

var app = builder.Build();

// Configure the HTTP request pipeline
// Enable Swagger in all environments
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "OEM API V1");
    c.RoutePrefix = "swagger";
});

// Only redirect to HTTPS in development (Azure handles HTTPS termination)
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowAll");

// Dev Authentication Middleware (US 4.1.1)
app.UseMiddleware<DevAuthenticationMiddleware>();

// Health check endpoints
app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.MapControllers();

app.Run();

// Make the implicit Program class public for testing
public partial class Program { }
