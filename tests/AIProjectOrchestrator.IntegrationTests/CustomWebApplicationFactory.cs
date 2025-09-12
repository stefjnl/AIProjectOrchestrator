using AIProjectOrchestrator.API;
using AIProjectOrchestrator.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AIProjectOrchestrator.IntegrationTests
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Add test-specific configuration
                var projectDir = Directory.GetCurrentDirectory();
                var appSettingsPath = Path.Combine(projectDir, "appsettings.json");
                if (File.Exists(appSettingsPath))
                {
                    config.AddJsonFile(appSettingsPath);
                }

                // Override connection string for Docker testing
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=aiprojectorchestrator;Username=user;Password=password"
                });
            });

            builder.ConfigureServices(services =>
            {
                // Remove any existing logging providers to reduce test output noise
                services.RemoveAll<ILoggerProvider>();
            });
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            // Configure the host to use test environment
            builder.UseEnvironment("IntegrationTests");
            var host = base.CreateHost(builder);
            
            // Ensure database is ready before returning the host
            var scope = host.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            context.Database.EnsureCreatedAsync().GetAwaiter().GetResult();
            
            return host;
        }

        public async Task EnsureDatabaseReadyAsync()
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.Database.EnsureCreatedAsync();
        }
    }
}
