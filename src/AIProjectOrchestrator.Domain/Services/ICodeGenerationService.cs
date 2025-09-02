using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Models.Code;

namespace AIProjectOrchestrator.Domain.Services
{
    public interface ICodeGenerationService
    {
        Task<CodeGenerationResponse> GenerateCodeAsync(CodeGenerationRequest request, CancellationToken cancellationToken = default);
        Task<CodeGenerationStatus> GetGenerationStatusAsync(Guid generationId, CancellationToken cancellationToken = default);
        Task<List<CodeArtifact>?> GetGenerationResultsAsync(Guid generationId, CancellationToken cancellationToken = default);
        Task<bool> CanGenerateCodeAsync(Guid storyGenerationId, CancellationToken cancellationToken = default);
        Task<List<CodeArtifact>> GetGeneratedFilesAsync(Guid generationId, CancellationToken cancellationToken = default);
        Task<byte[]?> GetGeneratedFilesZipAsync(Guid generationId, CancellationToken cancellationToken = default);
    }
}