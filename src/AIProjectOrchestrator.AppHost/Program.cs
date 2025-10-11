var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.AIProjectOrchestrator_API>("aiprojectorchestrator-api");

builder.Build().Run();
