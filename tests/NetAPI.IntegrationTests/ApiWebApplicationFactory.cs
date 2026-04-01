using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetAPI.Infrastructure.Persistence;

namespace NetAPI.IntegrationTests;

/// <summary>
/// Bootstraps the API with an in-memory EF Core database, replacing the real SQL Server.
/// JWT key is overridden to a known test secret for generating test tokens.
/// </summary>
public class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            // Override JWT and connection config for tests
            cfg.Sources.Clear();
            cfg.AddJsonFile("appsettings.json", optional: true);
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "TestDb",
                ["ConnectionStrings:Redis"] = "",
                ["Jwt:Key"] = "integration-test-super-secret-key-32chars!!",
                ["Jwt:Issuer"] = "NetAPI",
                ["Jwt:Audience"] = "NetAPI.Client",
                ["IpRateLimiting:EnableEndpointRateLimiting"] = "false",
                ["IpRateLimiting:GeneralRules:0:Endpoint"] = "*",
                ["IpRateLimiting:GeneralRules:0:Period"] = "1m",
                ["IpRateLimiting:GeneralRules:0:Limit"] = "10000"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the SQL Server DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            // Replace with in-memory database
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid()));
        });
    }
}
