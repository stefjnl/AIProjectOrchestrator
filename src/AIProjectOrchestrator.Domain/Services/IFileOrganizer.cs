using System.Collections.Generic;
using AIProjectOrchestrator.Domain.Models.Code;
using AIProjectOrchestrator.Domain.Models.Review;

namespace AIProjectOrchestrator.Domain.Services;

public interface IFileOrganizer
{
    List<CodeArtifact> OrganizeGeneratedFiles(List<CodeArtifact> files);

    string DetermineFileType(string fileName);

    string SerializeCodeArtifacts(List<CodeArtifact> artifacts);

    string GenerateImplementationGuide(CodeGenerationResponse response);

    Task<byte[]?> GetGeneratedFilesZipAsync(
        Guid generationId,
        List<CodeArtifact> generatedFiles,
        List<CodeArtifact> testFiles,
        CancellationToken cancellationToken = default);
}