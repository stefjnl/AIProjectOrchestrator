using AIProjectOrchestrator.ServiceDefaults;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Serilog;
using Serilog.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AIProjectOrchestrator.Infrastructure.Data;
using AIProjectOrchestrator.Application.Services;
using AIProjectOrchestrator.Application.Services.Builders;
using AIProjectOrchestrator.Application.Services.Handlers;
using AIProjectOrchestrator.Application.Services.Orchestrators;
using AIProjectOrchestrator.Application.Services.Parsers;
using AIProjectOrchestrator.Application.Services.Validators;
using AIProjectOrchestrator.Domain.Interfaces;
using AIProjectOrchestrator.Infrastructure.Repositories;
using AIProjectOrchestrator.Application.Interfaces;
using AIProjectOrchestrator.Domain.Services;
using AIProjectOrchestrator.Domain.Configuration;
using AIProjectOrchestrator.Domain.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using AIProjectOrchestrator.Domain.Models.AI;
using AIProjectOrchestrator.Infrastructure.AI;
using AIProjectOrchestrator.Infrastructure.Configuration;
using AIProjectOrchestrator.API.HealthChecks;
using AIProjectOrchestrator.Domain.Models.Review;
using AIProjectOrchestrator.API.Middleware;
using AIProjectOrchestrator.API.Configuration;
using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Configure SSL certificate validation for development/production environments via HttpClient handlers
// The ServicePointManager is obsolete, so we'll configure this via HttpClientHandler instead
// This is handled in the HttpClient configurations below

// Enhanced Serilog configuration for structured logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "AIProjectOrchestrator")
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File("logs/app-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// Add exception handling configuration
builder.Services.Configure<ExceptionHandlingOptions>(builder.Configuration.GetSection("ExceptionHandling"));

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

// Add health checks - only include working providers to avoid container health issues
builder.Services.AddHealthChecks()
    .AddCheck<OpenRouterHealthCheck>("openrouter")
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

// Register new story generation components
builder.Services.AddScoped<IStoryDependencyValidator, StoryDependencyValidator>();
builder.Services.AddScoped<IStoryContextBuilder, StoryContextBuilder>();
builder.Services.AddScoped<IStoryAIOrchestrator, StoryAIOrchestrator>();
builder.Services.AddScoped<IStoryParser, StoryParser>();
builder.Services.AddScoped<IStoryPersistenceHandler, StoryPersistenceHandler>();

builder.Services.AddScoped<ICodeGenerationService, CodeGenerationService>();
builder.Services.AddScoped<IPromptGenerationService, PromptGenerationService>();
builder.Services.AddScoped<PromptContextAssembler>();
builder.Services.AddScoped<ContextOptimizer>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IPromptTemplateRepository, PromptTemplateRepository>();
builder.Services.AddScoped<IPromptTemplateService, PromptTemplateService>();

// Unified AI configuration is bound below. Removed legacy bindings to avoid duplication.

// Add AI model configuration service
builder.Services.AddSingleton<AIProviderConfigurationService>();
builder.Services.AddSingleton<AIClientFallbackService>();

// Add code generation specialized services
builder.Services.AddScoped<ITestGenerator, TestGenerator>();
builder.Services.AddScoped<IImplementationGenerator, ImplementationGenerator>();
builder.Services.AddScoped<ICodeValidator, CodeValidator>();
builder.Services.AddScoped<IContextRetriever, ContextRetriever>();
builder.Services.AddScoped<IFileOrganizer, FileOrganizer>();

// Add refactored code generation services (Issue #5 - SRP compliance)
builder.Services.AddScoped<IWorkflowDependencyValidator, WorkflowDependencyValidator>();
builder.Services.AddScoped<ICodeGenerationOrchestrator, CodeGenerationOrchestrator>();
builder.Services.AddSingleton<ICodeGenerationStateManager, CodeGenerationStateManager>();

// Add instruction service configuration
builder.Services.Configure<InstructionSettings>(
    builder.Configuration.GetSection(InstructionSettings.SectionName));

// Add instruction service
builder.Services.AddSingleton<IInstructionService, InstructionService>();

// Configure AI Provider settings with operation-specific configurations
// Configure AI operation settings for provider selection and parameters
builder.Services.Configure<AIProjectOrchestrator.Infrastructure.Configuration.AIOperationSettings>(
    builder.Configuration.GetSection("AIProviders"));

// Configure domain AI Provider credentials from unified 'AIProviders:Providers' section
builder.Services.Configure<AIProjectOrchestrator.Domain.Configuration.AIProviderCredentials>(
    builder.Configuration.GetSection("AIProviders").GetSection("Providers"));

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
    var testSettings = builder.Configuration.GetSection("AIProviders").Get<AIProjectOrchestrator.Infrastructure.Configuration.AIOperationSettings>();
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
    .ConfigurePrimaryHttpMessageHandler(() => 
    {
        var handler = new HttpClientHandler
        {
            SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13,
            UseCookies = false,
            UseProxy = false
        };

        // Only bypass SSL validation in Development for testing
        if (builder.Environment.IsDevelopment())
        {
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            Log.Warning("Claude: SSL certificate validation disabled - DEVELOPMENT ONLY");
        }
        // In production, use proper SSL validation (default behavior)

        return handler;
    });

builder.Services.AddHttpClient<LMStudioClient>()
    .ConfigureHttpClient((serviceProvider, client) =>
    {
        var settings = serviceProvider.GetRequiredService<IOptions<AIProjectOrchestrator.Domain.Configuration.AIProviderCredentials>>().Value.LMStudio;
        client.BaseAddress = new Uri(settings.BaseUrl);
        // Set a longer base timeout to handle large prompts - we'll manage individual request timeouts in the client
        client.Timeout = TimeSpan.FromSeconds(Math.Max(settings.TimeoutSeconds, AIConstants.MinimumTimeoutSeconds)); // Minimum 2 minutes, max from config
    })
    .ConfigurePrimaryHttpMessageHandler(() => 
    {
        var handler = new HttpClientHandler
        {
            SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13,
            UseCookies = false,
            UseProxy = false
        };

        // LMStudio is typically a local service, allow bypass in Development
        if (builder.Environment.IsDevelopment())
        {
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            Log.Warning("LMStudio: SSL certificate validation disabled - DEVELOPMENT ONLY (local service)");
        }

        return handler;
    });

builder.Services.AddHttpClient<OpenRouterClient>()
    .ConfigureHttpClient((serviceProvider, client) =>
    {
        var settings = serviceProvider.GetRequiredService<IOptions<AIProjectOrchestrator.Domain.Configuration.AIProviderCredentials>>().Value.OpenRouter;
        // Ensure BaseAddress ends with trailing slash for proper URL construction
        var baseUrl = settings.BaseUrl.TrimEnd('/') + "/";
        client.BaseAddress = new Uri(baseUrl);
        // Set a longer base timeout to handle large prompts - we'll manage individual request timeouts in the client
        client.Timeout = TimeSpan.FromSeconds(Math.Max(settings.TimeoutSeconds, AIConstants.MinimumTimeoutSeconds)); // Minimum 2 minutes, max from config
    })
    .ConfigurePrimaryHttpMessageHandler(() => 
    {
        var handler = new HttpClientHandler
        {
            SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13,
            UseCookies = false,
            UseProxy = false
        };

        // OpenRouter is a public HTTPS service - should NEVER bypass SSL in production
        if (builder.Environment.IsDevelopment())
        {
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            Log.Warning("OpenRouter: SSL certificate validation disabled - DEVELOPMENT ONLY");
        }
        // In production, proper SSL validation is enforced (default behavior)

        return handler;
    });

builder.Services.AddHttpClient<NanoGptClient>()
    .ConfigureHttpClient((serviceProvider, client) =>
    {
        var settings = serviceProvider.GetRequiredService<IOptions<AIProjectOrchestrator.Domain.Configuration.AIProviderCredentials>>().Value.NanoGpt;
        client.BaseAddress = new Uri(settings.BaseUrl);
        // Set a longer base timeout to handle large prompts - we'll manage individual request timeouts in the client
        client.Timeout = TimeSpan.FromSeconds(Math.Max(settings.TimeoutSeconds, AIConstants.MinimumTimeoutSeconds)); // Minimum 2 minutes, max from config
    })
    .ConfigurePrimaryHttpMessageHandler(() => 
    {
        var handler = new HttpClientHandler
        {
            SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13,
            UseCookies = false,
            UseProxy = false
        };

        // NanoGpt proxy is typically a local service, allow bypass in Development
        if (builder.Environment.IsDevelopment())
        {
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            Log.Warning("NanoGpt: SSL certificate validation disabled - DEVELOPMENT ONLY (local proxy)");
        }

        return handler;
    });

// Register Docker-specific HttpClient for new AI providers
builder.Services.AddHttpClient("DockerAIClient")
    .ConfigurePrimaryHttpMessageHandler(() => 
    {
        var handler = new HttpClientHandler
        {
            SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13,
            UseCookies = false,
            UseProxy = false,
            AutomaticDecompression = System.Net.DecompressionMethods.All
        };

        // Only bypass SSL in Development for Docker/container local services
        if (builder.Environment.IsDevelopment())
        {
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
            {
                if (errors != SslPolicyErrors.None)
                {
                    Log.Warning("DockerAIClient: SSL validation bypass in Development - Errors: {Errors}", errors);
                    if (cert != null)
                    {
                        Log.Debug("Certificate: {Subject} issued by {Issuer}", cert.Subject, cert.Issuer);
                    }
                }
                return true; // Allow bypass in Development only
            };
        }
        // In production, proper SSL validation is enforced

        return handler;
    })
    .ConfigureHttpClient((serviceProvider, client) =>
    {
        client.Timeout = TimeSpan.FromMinutes(5);
        client.DefaultRequestHeaders.Add("User-Agent", "AIProjectOrchestrator-Docker/1.0");
        client.DefaultRequestHeaders.ConnectionClose = false;
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

// Register exception middleware first in the pipeline
app.UseMiddleware<ExceptionMiddleware>();

app.UseHttpsRedirection();

// Enable response compression
app.UseResponseCompression();

// Enable CORS
app.UseCors("AllowFrontend");

app.UseDefaultFiles();
app.UseStaticFiles();

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