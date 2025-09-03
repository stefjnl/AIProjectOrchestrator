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

var builder = WebApplication.CreateBuilder(args);

// Add Serilog logging
builder.Host.UseSerilog((context, configuration) =>
    configuration.WriteTo.Console()
        .ReadFrom.Configuration(context.Configuration));

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

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

// Add services
builder.Services.AddScoped<IProjectService, ProjectService>();

// Add requirements analysis service
builder.Services.AddScoped<IRequirementsAnalysisService, RequirementsAnalysisService>();

// Add project planning service
builder.Services.AddScoped<IProjectPlanningService, ProjectPlanningService>();

// Add story generation service
builder.Services.AddScoped<IStoryGenerationService, StoryGenerationService>();

// Add code generation service
builder.Services.AddScoped<ICodeGenerationService, CodeGenerationService>();

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

// Register HTTP clients for each provider
builder.Services.AddHttpClient<ClaudeClient>()
    .ConfigureHttpClient((serviceProvider, client) => {
        var settings = serviceProvider.GetRequiredService<IOptions<AIProviderSettings>>().Value.Claude;
        client.BaseAddress = new Uri(settings.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
    });

builder.Services.AddHttpClient<LMStudioClient>()
    .ConfigureHttpClient((serviceProvider, client) => {
        var settings = serviceProvider.GetRequiredService<IOptions<AIProviderSettings>>().Value.LMStudio;
        client.BaseAddress = new Uri(settings.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
    });

builder.Services.AddHttpClient<OpenRouterClient>()
    .ConfigureHttpClient((serviceProvider, client) => {
        var settings = serviceProvider.GetRequiredService<IOptions<AIProviderSettings>>().Value.OpenRouter;
        client.BaseAddress = new Uri(settings.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
    });

// Register AI clients as singletons
builder.Services.AddSingleton<IAIClient, ClaudeClient>();
builder.Services.AddSingleton<IAIClient, LMStudioClient>();
builder.Services.AddSingleton<IAIClient, OpenRouterClient>();

// Register factory for accessing specific clients
builder.Services.AddSingleton<IAIClientFactory, AIClientFactory>();

// Register Review service as singleton (for in-memory storage consistency)
builder.Services.AddSingleton<IReviewService, ReviewService>();

// Register background cleanup service
builder.Services.AddHostedService<ReviewCleanupService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Enable static file serving
app.UseStaticFiles();

// Map controllers
app.MapControllers();

// Map health checks endpoint
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }
