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
        Task<CodeGenerationStatus> GetStatusAsync(Guid codeGenerationId, CancellationToken cancellationToken = default);
        Task<CodeArtifactsResult> GetGeneratedCodeAsync(Guid codeGenerationId, CancellationToken cancellationToken = default);
        Task<bool> CanGenerateCodeAsync(Guid storyGenerationId, CancellationToken cancellationToken = default);
        Task<byte[]?> GetGeneratedFilesZipAsync(Guid generationId, CancellationToken cancellationToken = default);
    }
}