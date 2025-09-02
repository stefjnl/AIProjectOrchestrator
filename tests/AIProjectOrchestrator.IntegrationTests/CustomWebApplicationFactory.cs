using AIProjectOrchestrator.API;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

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
            return base.CreateHost(builder);
        }
    }
}