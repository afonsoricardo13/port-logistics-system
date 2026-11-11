using Microsoft.EntityFrameworkCore;
using Application.Services;
using DataModel.Repository;
using DataModel.Mapper;
using Domain.Factory;
using Domain.IRepository;
using WebApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Optional Razor/static UI: we'll serve simple static pages from wwwroot
// No Razor Pages required for now; we'll use plain HTML + fetch()

// CORS Configuration for Angular SPA and external access
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policyBuilder =>
        {
            // Allow localhost (development) and Azure production origins
            policyBuilder
                .SetIsOriginAllowed(origin => 
                    origin.Contains("localhost") || 
                    origin.Contains("127.0.0.1") ||
                    origin.Contains("portlogisticsspa.z1.web.core.windows.net"))
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials()
                .WithExposedHeaders("Content-Disposition");
        });
});

builder.Services.AddDbContext<AbsanteeContext>(opt => {
    // Check if we're running integration tests
    var isIntegrationTest = Environment.GetEnvironmentVariable("ASPNETCORE_INTEGRATION_TEST") == "true";

    // ALWAYS use Azure MySQL (removed SQLite)
    if (!isIntegrationTest)
    {
        // MySQL Configuration - ALWAYS Azure MySQL
        var connectionString = builder.Configuration.GetConnectionString("AbsanteeDatabase");

        // Use a fixed MySQL version to avoid connection attempts during configuration
        var serverVersion = new MySqlServerVersion(new Version(8, 0, 21));
        opt.UseMySql(connectionString, serverVersion);
    }
    // If integration test, the factory will register SQLite via ConfigureServices
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("X-User-Email", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "User email for development authentication (e.g., admin@example.com for Administrator, operator@example.com for LogisticsOperator)",
        Name = "X-User-Email",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "X-User-Email"
    });
    
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "X-User-Email"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Docks registrations
builder.Services.AddTransient<IDockRepository, DockRepository>();
builder.Services.AddTransient<IDockFactory, DockFactory>();
builder.Services.AddTransient<DockMapper>();
builder.Services.AddTransient<DockService>();

// OperatingStaff registrations
builder.Services.AddTransient<IOperatingStaffRepository, OperatingStaffRepository>();
builder.Services.AddTransient<IOperatingStaffFactory, OperatingStaffFactory>();
builder.Services.AddTransient<OperatingStaffMapper>();
builder.Services.AddTransient<OperatingStaffService>();

// Qualification registrations (US 2.2.13)
builder.Services.AddTransient<IQualificationRepository, QualificationRepository>();
builder.Services.AddTransient<QualificationMapper>();
builder.Services.AddTransient<QualificationService>();

// Vessel registrations
builder.Services.AddTransient<IVesselRepository, VesselRepository>();
builder.Services.AddTransient<IVesselFactory, VesselFactory>();
builder.Services.AddTransient<VesselMapper>();
builder.Services.AddTransient<VesselService>();

// Vessel Type registrations (US 2.2.1)
builder.Services.AddTransient<IVesselTypeRepository, VesselTypeRepository>();
builder.Services.AddTransient<IVesselTypeFactory, VesselTypeFactory>();
builder.Services.AddTransient<VesselTypeMapper>();
builder.Services.AddTransient<VesselTypeService>();

// Vessel Visit Notification registrations
builder.Services.AddTransient<IVesselVisitNotificationRepository, VesselVisitNotificationRepository>();
builder.Services.AddTransient<VesselVisitNotificationMapper>();
builder.Services.AddTransient<VesselVisitNotificationService>();

builder.Services.AddTransient<IPhysicalResourceRepository, PhysicalResourceRepository>();
builder.Services.AddTransient<PhysicalResourceMapper>();
builder.Services.AddTransient<PhysicalResourceService>();

// Storage Areas (US 2.2.4)
builder.Services.AddTransient<IStorageAreaRepository, StorageAreaRepository>();
builder.Services.AddTransient<StorageAreaMapper>();
builder.Services.AddTransient<StorageAreaService>();

// Organizations and Representatives (US 2.2.5, US 2.2.6)
builder.Services.AddTransient<IOrganizationRepository, OrganizationRepository>();
builder.Services.AddTransient<IRepresentativeRepository, RepresentativeRepository>();
builder.Services.AddTransient<IOrganizationFactory, OrganizationFactory>();
builder.Services.AddTransient<IRepresentativeFactory, RepresentativeFactory>();
builder.Services.AddTransient<RepresentativeMapper>();
builder.Services.AddTransient<OrganizationMapper>();
builder.Services.AddTransient<OrganizationService>();
builder.Services.AddTransient<RepresentativeService>();

// Port Layout for 3D Visualization (US 3.3.2)
builder.Services.AddTransient<PortLayoutService>();

// Authorization (US 3.2.2)
builder.Services.AddTransient<AuthorizationService>();

// User Management (US 3.2.5, US 3.2.6)
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddTransient<IUserManagementService, UserManagementService>();

// Role Assignment Service (US 3.2.2, US 3.2.4)
// Changed to Scoped because it now depends on AbsanteeContext (scoped DbContext)
builder.Services.AddScoped<RoleAssignmentService>();

// Dock Rebalancing Service (US 4.3.3)
builder.Services.AddTransient<DockRebalanceService>();

// Privacy Policy Service (US 4.5.1 - GDPR Compliance)
builder.Services.AddTransient<IPrivacyPolicyRepository, PrivacyPolicyRepository>();
builder.Services.AddTransient<IPrivacyPolicyService, PrivacyPolicyService>();

// Data Rights Service (US 4.5.3 - GDPR Data Rights)
builder.Services.AddTransient<IDataRightsRequestRepository, DataRightsRequestRepository>();
builder.Services.AddTransient<IDataRightsService, DataRightsService>();

// HTTP Client for external services (Scheduling server, OEM API)
builder.Services.AddHttpClient();

var app = builder.Build();

// Apply pending EF Core migrations at startup (safe for single-instance dev/azure)
// Skip migrations during integration tests (test factory handles db initialization)
var isIntegrationTest = Environment.GetEnvironmentVariable("ASPNETCORE_INTEGRATION_TEST") == "true";
if (!isIntegrationTest)
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AbsanteeContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        // This will create the database and any missing tables based on migrations
        db.Database.Migrate();
    }
}

// Configure the HTTP request pipeline.
// Enable Swagger in all environments (including Production for Azure)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Port Logistics API v1");
    c.RoutePrefix = "swagger"; // Access Swagger at /swagger
});

// Only redirect to HTTPS in development (Azure handles HTTPS termination)
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
} 

// CRITICAL: Routing must come before CORS
app.UseRouting();

// Enable CORS - must come AFTER UseRouting and BEFORE UseAuthorization
app.UseCors("AllowAll");

// Dev Authentication Middleware (US 3.2.2, US 3.2.4)
app.UseMiddleware<DevAuthenticationMiddleware>();

// Authorization must come AFTER CORS
app.UseAuthorization();

// Serve default documents (e.g., index.html) and static files from wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

// Health check endpoint
app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.MapControllers();

app.Run();

public partial class Program { }