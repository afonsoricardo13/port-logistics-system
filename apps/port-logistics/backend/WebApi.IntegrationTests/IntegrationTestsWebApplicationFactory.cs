using System.Data.Common;
using DataModel.Repository;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace WebApi.IntegrationTests;


public class IntegrationTestsWebApplicationFactory<Program>
    : WebApplicationFactory<Program> where Program : class
{
    private SqliteConnection? _connection;

    public IntegrationTestsWebApplicationFactory()
    {
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {

        // based on https://stackoverflow.com/questions/72679169/override-host-configuration-in-integration-testing-using-asp-net-core-6-minimal
        var configurationValues = new Dictionary<string, string?>
        {
            // Override connection string to use in-memory SQLite for tests
            ["ConnectionStrings:AbsanteeDatabase"] = "DataSource=:memory:"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues)
            .Build();

        // Set environment variable to signal we're running integration tests
        // This MUST be set before building the application
        Environment.SetEnvironmentVariable("ASPNETCORE_INTEGRATION_TEST", "true");

        builder
            // This configuration is used during the creation of the application
            // (e.g. BEFORE WebApplication.CreateBuilder(args) is called in Program.cs).
            .UseConfiguration(configuration)
            .ConfigureAppConfiguration(configurationBuilder =>
            {
                // This overrides configuration settings that were added as part 
                // of building the Host (e.g. calling WebApplication.CreateBuilder(args)).
                configurationBuilder.AddInMemoryCollection(configurationValues);
            });

        // Use ConfigureServices instead - it runs during service registration
        builder.ConfigureServices(services =>
        {
            // Create open SqliteConnection so EF won't automatically close it.
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            services.AddSingleton<DbConnection>(_connection);

            // Register SQLite DbContext (MySQL won't be registered by Program.cs)
            services.AddDbContext<AbsanteeContext>((container, options) =>
            {
                var connection = container.GetRequiredService<DbConnection>();
                options.UseSqlite(connection);
            });
        });

        builder.UseEnvironment("Development");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection?.Dispose();
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        
        // Initialize the database after the host is created
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AbsanteeContext>();
            db.Database.EnsureCreated();
        }
        
        return host;
    }

    /// <summary>
    /// Create HttpClient with authentication header for integration tests
    /// US 3.2.4: Tests need X-User-Email header for authorization
    /// </summary>
    public HttpClient CreateAuthenticatedClient(string email = "test@admin.com")
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-User-Email", email);
        return client;
    }
}