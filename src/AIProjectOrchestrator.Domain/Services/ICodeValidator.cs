using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Models.Code;

namespace AIProjectOrchestrator.Domain.Services;

public interface ICodeValidator
{
    Task<bool> ValidateGeneratedCodeAsync(List<CodeArtifact> artifacts, CancellationToken cancellationToken = default);

    Task<List<string>> ValidateCSharpSyntaxAsync(string codeContent, CancellationToken cancellationToken = default);
}