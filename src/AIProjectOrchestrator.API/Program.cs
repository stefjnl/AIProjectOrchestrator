using Serilog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AIProjectOrchestrator.Infrastructure.Data;
using AIProjectOrchestrator.Domain.Interfaces;
using AIProjectOrchestrator.Infrastructure.Repositories;
using AIProjectOrchestrator.Application.Interfaces;
using AIProjectOrchestrator.Application.Services;
using AIProjectOrchestrator.Domain.Services;
using AIProjectOrchestrator.Domain.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using AIProjectOrchestrator.Domain.Models.AI;
using AIProjectOrchestrator.Infrastructure.AI;
using AIProjectOrchestrator.API.HealthChecks;
using AIProjectOrchestrator.Domain.Models.Review;
using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);

// Add Serilog logging
builder.Host.UseSerilog((context, configuration) =>
    configuration.WriteTo.Console()
        .ReadFrom.Configuration(context.Configuration));

// Add services to the container.
builder.Services.AddControllers();
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

// Configure AI Provider settings
builder.Services.Configure<AIProviderSettings>(
    builder.Configuration.GetSection(AIProviderSettings.SectionName));

// Configure Review settings
builder.Services.Configure<ReviewSettings>(
    builder.Configuration.GetSection(ReviewSettings.SectionName));

// Register HTTP clients for each provider and AI clients as singletons
builder.Services.AddHttpClient<ClaudeClient>()
    .ConfigureHttpClient((serviceProvider, client) =>
    {
        var settings = serviceProvider.GetRequiredService<IOptions<AIProviderSettings>>().Value.Claude;
        client.BaseAddress = new Uri(settings.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
    });

builder.Services.AddHttpClient<LMStudioClient>()
    .ConfigureHttpClient((serviceProvider, client) =>
    {
        var settings = serviceProvider.GetRequiredService<IOptions<AIProviderSettings>>().Value.LMStudio;
        client.BaseAddress = new Uri(settings.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
    });

builder.Services.AddHttpClient<OpenRouterClient>()
    .ConfigureHttpClient((serviceProvider, client) =>
    {
        var settings = serviceProvider.GetRequiredService<IOptions<AIProviderSettings>>().Value.OpenRouter;
        // Ensure BaseAddress ends with trailing slash for proper URL construction
        var baseUrl = settings.BaseUrl.TrimEnd('/') + "/";
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
    });

// Register AI clients as singletons - OpenRouter uses the named HttpClient
builder.Services.AddSingleton<IAIClient>(serviceProvider =>
{
    var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient(nameof(OpenRouterClient));
    var logger = serviceProvider.GetRequiredService<ILogger<OpenRouterClient>>();
    var settings = serviceProvider.GetRequiredService<IOptions<AIProviderSettings>>();
    logger.LogInformation("Creating OpenRouterClient with HttpClient BaseAddress: {BaseAddress}", httpClient.BaseAddress?.ToString() ?? "NULL");
    return new OpenRouterClient(httpClient, logger, settings);
});

builder.Services.AddSingleton<IAIClient, ClaudeClient>();
builder.Services.AddSingleton<IAIClient, LMStudioClient>();

// Register factory for accessing specific clients
builder.Services.AddSingleton<IAIClientFactory, AIClientFactory>();

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

// Map health checks endpoint
app.MapHealthChecks("/api/health");

// SPA fallback routing - serve index.html for non-API routes
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program { }

