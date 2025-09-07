using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Models.Code;
using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Models;

namespace AIProjectOrchestrator.Domain.Services;

public interface IImplementationGenerator
{
    Task<List<CodeArtifact>> GenerateImplementationAsync(
        string instructionContent,
        ComprehensiveContext context,
        List<CodeArtifact> testFiles,
        string selectedModel,
        CancellationToken cancellationToken = default);
}