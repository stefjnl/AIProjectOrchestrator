using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Models.Code;
using AIProjectOrchestrator.Domain.Models.AI;
using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Models;

namespace AIProjectOrchestrator.Domain.Services;

public interface ITestGenerator
{
    Task<List<CodeArtifact>> GenerateTestFilesAsync(
        string instructionContent,
        ComprehensiveContext context,
        string selectedModel,
        CancellationToken cancellationToken = default);
}