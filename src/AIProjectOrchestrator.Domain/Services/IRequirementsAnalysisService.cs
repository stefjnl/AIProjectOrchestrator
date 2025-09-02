using System;
using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Models;

namespace AIProjectOrchestrator.Domain.Services
{
    public interface IRequirementsAnalysisService
    {
        Task<RequirementsAnalysisResponse> AnalyzeRequirementsAsync(
            RequirementsAnalysisRequest request,
            CancellationToken cancellationToken = default);

        Task<RequirementsAnalysisStatus> GetAnalysisStatusAsync(
            Guid analysisId,
            CancellationToken cancellationToken = default);

        Task<RequirementsAnalysisResponse?> GetAnalysisResultsAsync(
            Guid analysisId,
            CancellationToken cancellationToken = default);

        Task<string?> GetAnalysisResultContentAsync(
            Guid analysisId,
            CancellationToken cancellationToken = default);

        Task<bool> CanAnalyzeRequirementsAsync(
            Guid projectId,
            CancellationToken cancellationToken = default);
            
        Task<string?> GetBusinessContextAsync(Guid analysisId, CancellationToken cancellationToken = default);
    }
}
