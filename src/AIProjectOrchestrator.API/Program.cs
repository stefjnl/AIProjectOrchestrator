using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Serilog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AIProjectOrchestrator.Infrastructure.Data;
using AIProjectOrchestrator.Application.Services;
using AIProjectOrchestrator.Domain.Interfaces;
using AIProjectOrchestrator.Infrastructure.Repositories;
using AIProjectOrchestrator.Application.Interfaces;
using AIProjectOrchestrator.Domain.Services;
using AIProjectOrchestrator.Domain.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using AIProjectOrchestrator.Domain.Models.AI;
using AIProjectOrchestrator.Infrastructure.AI;
using AIProjectOrchestrator.Infrastructure.Configuration;
using AIProjectOrchestrator.API.HealthChecks;
using AIProjectOrchestrator.Domain.Models.Review;
using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);

// Configure SSL certificate validation for development/production environments
if (builder.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("DisableSslValidation"))
{
    // Allow all certificates in development or when explicitly disabled
    ServicePointManager.ServerCertificateValidationCallback =
        (sender, certificate, chain, sslPolicyErrors) => true;
}
else
{
    // In production, implement proper certificate validation
    ServicePointManager.ServerCertificateValidationCallback =
        (sender, certificate, chain, sslPolicyErrors) =>
        {
            // Log SSL validation issues for debugging
            if (sslPolicyErrors != SslPolicyErrors.None)
            {
                Console.WriteLine($"SSL Validation Error: {sslPolicyErrors}");
                if (certificate != null)
                {
                    Console.WriteLine($"Certificate Subject: {certificate.Subject}");
                    Console.WriteLine($"Certificate Issuer: {certificate.Issuer}");
                }
            }
            // For now, allow all certificates - in production, implement proper validation
            return true;
        };
}

// Add Serilog logging
builder.Host.UseSerilog((context, configuration) =>
    configuration.WriteTo.Console()
        .ReadFrom.Configuration(context.Configuration));

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddOpenApi();

// Add response compression for large LLM responses
builder.Services.AddResponseCompression(options =>
{
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/json", "text/plain" });
});

// Add CORS policy for frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck<ClaudeHealthCheck>("claude")
    .AddCheck<LMStudioHealthCheck>("lmstudio")
    .AddCheck<OpenRouterHealthCheck>("openrouter")
    .AddCheck<NanoGptHealthCheck>("nanogpt")
    .AddCheck<ReviewHealthCheck>("review");

// Add Entity Framework
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add repositories
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IRequirementsAnalysisRepository, RequirementsAnalysisRepository>();
builder.Services.AddScoped<IProjectPlanningRepository, ProjectPlanningRepository>();
builder.Services.AddScoped<IStoryGenerationRepository, StoryGenerationRepository>();
builder.Services.AddScoped<IPromptGenerationRepository, PromptGenerationRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();

// Add services
builder.Services.AddScoped<IProjectService, ProjectService>();

// Add requirements analysis service
builder.Services.AddScoped<IRequirementsAnalysisService, RequirementsAnalysisService>();
builder.Services.AddScoped<IProjectPlanningService, ProjectPlanningService>();
builder.Services.AddScoped<IStoryGenerationService, StoryGenerationService>();
builder.Services.AddScoped<ICodeGenerationService, CodeGenerationService>();
builder.Services.AddScoped<IPromptGenerationService, PromptGenerationService>();
builder.Services.AddScoped<PromptContextAssembler>();
builder.Services.AddScoped<ContextOptimizer>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IPromptTemplateRepository, PromptTemplateRepository>();
builder.Services.AddScoped<IPromptTemplateService, PromptTemplateService>();

// Add AI provider credentials configuration
builder.Services.Configure<AIProviderCredentials>(
    builder.Configuration.GetSection(AIProviderCredentials.SectionName));

// Add AI operation settings configuration
builder.Services.Configure<AIOperationSettings>(
    builder.Configuration.GetSection("AIOperations"));

// Add AI model configuration service
builder.Services.AddSingleton<AIProviderConfigurationService>();
builder.Services.AddSingleton<AIClientFallbackService>();

// Add code generation specialized services
builder.Services.AddScoped<ITestGenerator, TestGenerator>();
builder.Services.AddScoped<IImplementationGenerator, ImplementationGenerator>();
builder.Services.AddScoped<ICodeValidator, CodeValidator>();
builder.Services.AddScoped<IContextRetriever, ContextRetriever>();
builder.Services.AddScoped<IFileOrganizer, FileOrganizer>();

// Add instruction service configuration
builder.Services.Configure<InstructionSettings>(
    builder.Configuration.GetSection(InstructionSettings.SectionName));

// Add instruction service
builder.Services.AddSingleton<IInstructionService, InstructionService>();

// Configure AI Provider settings with operation-specific configurations
// Configure AI operation settings for provider selection and parameters
builder.Services.Configure<AIProjectOrchestrator.Infrastructure.Configuration.AIOperationSettings>(
    builder.Configuration.GetSection("AIOperations"));

// Configure domain AI Provider settings for API keys and base URLs
builder.Services.Configure<AIProjectOrchestrator.Domain.Configuration.AIProviderCredentials>(
    builder.Configuration.GetSection("AIProviderConfigurations"));

// Log the configuration for debugging
var aiProvidersSection = builder.Configuration.GetSection("AIProviders");
var operationsSection = aiProvidersSection.GetSection("Operations");
Console.WriteLine($"AI Providers section exists: {aiProvidersSection.Exists()}");
Console.WriteLine($"Operations section exists: {operationsSection.Exists()}");
if (operationsSection.Exists())
{
    var operationNames = operationsSection.GetChildren().Select(child => child.Key).ToList();
    Console.WriteLine($"Available operations: {string.Join(", ", operationNames)}");
}

// Test configuration binding
try
{
    var testSettings = builder.Configuration.GetSection("AIOperations").Get<AIProjectOrchestrator.Infrastructure.Configuration.AIOperationSettings>();
    if (testSettings != null && testSettings.Operations != null)
    {
        Console.WriteLine($"Configuration binding test successful. Operations count: {testSettings.Operations.Count}");
        Console.WriteLine($"Operations: {string.Join(", ", testSettings.Operations.Keys)}");
    }
    else
    {
        Console.WriteLine("Configuration binding test failed - null settings or operations");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Configuration binding test failed: {ex.Message}");
}

// Configure Review settings
builder.Services.Configure<ReviewSettings>(
    builder.Configuration.GetSection(ReviewSettings.SectionName));

// Register HTTP clients for each provider and AI clients as singletons
builder.Services.AddHttpClient<ClaudeClient>()
    .ConfigureHttpClient((serviceProvider, client) =>
    {
        var settings = serviceProvider.GetRequiredService<IOptions<AIProjectOrchestrator.Domain.Configuration.AIProviderCredentials>>().Value.Claude;
        client.BaseAddress = new Uri(settings.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
        SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13,
        UseCookies = false,
        UseProxy = false,
        CheckCertificateRevocationList = false
    });

builder.Services.AddHttpClient<LMStudioClient>()
    .ConfigureHttpClient((serviceProvider, client) =>
    {
        var settings = serviceProvider.GetRequiredService<IOptions<AIProjectOrchestrator.Domain.Configuration.AIProviderCredentials>>().Value.LMStudio;
        client.BaseAddress = new Uri(settings.BaseUrl);
        // Set a longer base timeout to handle large prompts - we'll manage individual request timeouts in the client
        client.Timeout = TimeSpan.FromSeconds(Math.Max(settings.TimeoutSeconds, 120)); // Minimum 2 minutes, max from config
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
        SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13,
        UseCookies = false,
        UseProxy = false,
        CheckCertificateRevocationList = false
    });

builder.Services.AddHttpClient<OpenRouterClient>()
    .ConfigureHttpClient((serviceProvider, client) =>
    {
        var settings = serviceProvider.GetRequiredService<IOptions<AIProjectOrchestrator.Domain.Configuration.AIProviderCredentials>>().Value.OpenRouter;
        // Ensure BaseAddress ends with trailing slash for proper URL construction
        var baseUrl = settings.BaseUrl.TrimEnd('/') + "/";
        client.BaseAddress = new Uri(baseUrl);
        // Set a longer base timeout to handle large prompts - we'll manage individual request timeouts in the client
        client.Timeout = TimeSpan.FromSeconds(Math.Max(settings.TimeoutSeconds, 120)); // Minimum 2 minutes, max from config
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
        SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13,
        UseCookies = false,
        UseProxy = false,
        CheckCertificateRevocationList = false
    });

builder.Services.AddHttpClient<NanoGptClient>()
    .ConfigureHttpClient((serviceProvider, client) =>
    {
        var settings = serviceProvider.GetRequiredService<IOptions<AIProjectOrchestrator.Domain.Configuration.AIProviderCredentials>>().Value.NanoGpt;
        client.BaseAddress = new Uri(settings.BaseUrl);
        // Set a longer base timeout to handle large prompts - we'll manage individual request timeouts in the client
        client.Timeout = TimeSpan.FromSeconds(Math.Max(settings.TimeoutSeconds, 120)); // Minimum 2 minutes, max from config
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
        SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13,
        UseCookies = false,
        UseProxy = false,
        CheckCertificateRevocationList = false
    });

// Register Docker-specific HttpClient for new AI providers with aggressive SSL bypass for container environments
builder.Services.AddHttpClient("DockerAIClient")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
        {
            // Log SSL validation issues for debugging - aggressive bypass for container environments
            if (errors != SslPolicyErrors.None)
            {
                Console.WriteLine($"Docker SSL Validation Bypass: {errors}");
                if (cert != null)
                {
                    Console.WriteLine($"Certificate: {cert.Subject} issued by {cert.Issuer}");
                }
            }
            return true; // Complete SSL bypass for Docker/container environments
        },
        SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13,
        UseCookies = false,
        UseProxy = false, // Bypass any proxy issues in container environments
        CheckCertificateRevocationList = false, // Skip CRL checks that might fail in containers
        AutomaticDecompression = System.Net.DecompressionMethods.All // Handle compression automatically
    })
    .ConfigureHttpClient((serviceProvider, client) =>
    {
        client.Timeout = TimeSpan.FromMinutes(5); // Longer timeout for Docker networking
        client.DefaultRequestHeaders.Add("User-Agent", "AIProjectOrchestrator-Docker/1.0");
        // Add additional headers to help with container networking
        client.DefaultRequestHeaders.ConnectionClose = false; // Keep connections alive
    });

// Register AI clients as singletons
builder.Services.AddSingleton<IAIClient>(serviceProvider =>
{
    var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient(nameof(NanoGptClient));
    var logger = serviceProvider.GetRequiredService<ILogger<NanoGptClient>>();
    var configurationService = serviceProvider.GetRequiredService<AIProviderConfigurationService>();
    logger.LogInformation("Creating NanoGptClient with HttpClient BaseAddress: {BaseAddress}", httpClient.BaseAddress?.ToString() ?? "NULL");
    return new NanoGptClient(httpClient, logger, configurationService);
});

builder.Services.AddSingleton<IAIClient>(serviceProvider =>
{
    var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient(nameof(OpenRouterClient));
    var logger = serviceProvider.GetRequiredService<ILogger<OpenRouterClient>>();
    var configurationService = serviceProvider.GetRequiredService<AIProviderConfigurationService>();
    logger.LogInformation("Creating OpenRouterClient with HttpClient BaseAddress: {BaseAddress}", httpClient.BaseAddress?.ToString() ?? "NULL");
    return new OpenRouterClient(httpClient, logger, configurationService);
});

builder.Services.AddSingleton<IAIClient, ClaudeClient>();
builder.Services.AddSingleton<IAIClient, LMStudioClient>();

// Register factory for accessing specific clients
builder.Services.AddSingleton<IAIClientFactory, AIClientFactory>();

// Register default provider service
builder.Services.AddSingleton<IDefaultProviderService, AIProjectOrchestrator.Application.Services.DefaultProviderService>();

// Register provider configuration service for Infrastructure layer
builder.Services.AddSingleton<IProviderConfigurationService, ProviderConfigurationService>();

// Register provider management service
builder.Services.AddScoped<IProviderManagementService, ProviderManagementService>();

// Register new service-specific AI providers (Clean Architecture approach)
// These providers will eliminate DRY violations by centralizing AI configuration
// They use IHttpClientFactory for proper Docker SSL support
builder.Services.AddScoped<AIProjectOrchestrator.Infrastructure.AI.Providers.IRequirementsAIProvider,
    AIProjectOrchestrator.Infrastructure.AI.Providers.RequirementsAIProvider>();
builder.Services.AddScoped<AIProjectOrchestrator.Infrastructure.AI.Providers.IPlanningAIProvider,
    AIProjectOrchestrator.Infrastructure.AI.Providers.PlanningAIProvider>();
builder.Services.AddScoped<AIProjectOrchestrator.Infrastructure.AI.Providers.IStoryAIProvider,
    AIProjectOrchestrator.Infrastructure.AI.Providers.StoryAIProvider>();
builder.Services.AddScoped<AIProjectOrchestrator.Infrastructure.AI.Providers.IPromptGenerationAIProvider,
    AIProjectOrchestrator.Infrastructure.AI.Providers.PromptGenerationAIProvider>();
builder.Services.AddScoped<AIProjectOrchestrator.Infrastructure.AI.Providers.ITestGenerationAIProvider,
    AIProjectOrchestrator.Infrastructure.AI.Providers.TestGenerationAIProvider>();
builder.Services.AddScoped<AIProjectOrchestrator.Infrastructure.AI.Providers.IImplementationGenerationAIProvider,
    AIProjectOrchestrator.Infrastructure.AI.Providers.ImplementationGenerationAIProvider>();

// Register Lazy<IReviewService> for services that need it
builder.Services.AddScoped<Lazy<IReviewService>>(serviceProvider => new Lazy<IReviewService>(() => serviceProvider.GetRequiredService<IReviewService>()));

// Register background cleanup service
builder.Services.AddHostedService<ReviewCleanupService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Apply migrations on startup in development
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Database.Migrate();
    }
}

app.UseHttpsRedirection();

// Enable response compression
app.UseResponseCompression();

// Enable CORS
app.UseCors("AllowFrontend");

// Enable default files (index.html) and static file serving
app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Cache static files for 1 hour in production
        if (!app.Environment.IsDevelopment())
        {
            ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=3600");
        }
    }
});

// Map controllers
app.MapControllers();

// Add Razor pages
app.MapRazorPages();

// Add MVC routing
app.MapDefaultControllerRoute();

// Map health checks endpoint
app.MapHealthChecks("/api/health");

// SPA fallback routing - serve index.html for non-API routes
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program { }
