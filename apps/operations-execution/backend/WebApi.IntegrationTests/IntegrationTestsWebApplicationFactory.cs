using System.Data.Common;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DataModel;

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
        var configurationValues = new Dictionary<string, string?>
        {
            // Ensure Program.cs reads this connection string, but we'll replace the registration below
            ["ConnectionStrings:OEMDatabase"] = "DataSource=:memory:"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues)
            .Build();

        Environment.SetEnvironmentVariable("ASPNETCORE_INTEGRATION_TEST", "true");

        builder
            .UseConfiguration(configuration)
            .ConfigureAppConfiguration(configurationBuilder =>
            {
                configurationBuilder.AddInMemoryCollection(configurationValues);
            });

        builder.ConfigureServices(services =>
        {
            // Remove existing OEMDbContext registrations (added by Program.cs using MySQL)
            // This prevents ServerVersion.AutoDetect(...) from attempting to connect to MySQL
            var dbContextOptionsDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(DbContextOptions<OEMDbContext>));
            if (dbContextOptionsDescriptor != null)
            {
                services.Remove(dbContextOptionsDescriptor);
            }

            var oemDbContextDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(OEMDbContext));
            if (oemDbContextDescriptor != null)
            {
                services.Remove(oemDbContextDescriptor);
            }

            // Create and open shared in-memory Sqlite connection
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            services.AddSingleton<DbConnection>(_connection);

            // Register OEMDbContext using the in-memory connection (this replaces MySQL registration)
            services.AddDbContext<OEMDbContext>((container, options) =>
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

        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OEMDbContext>();
            db.Database.EnsureCreated();
        }

        return host;
    }

    public HttpClient CreateAuthenticatedClient(string email = "test@integration.com")
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-User-Email", email);
        return client;
    }
}
