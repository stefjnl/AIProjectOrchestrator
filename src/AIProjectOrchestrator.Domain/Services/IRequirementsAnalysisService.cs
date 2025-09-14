using System;
using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Models;
using AIProjectOrchestrator.Domain.Entities;

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
            int projectId,
            CancellationToken cancellationToken = default);
            
        Task<string?> GetBusinessContextAsync(Guid analysisId, CancellationToken cancellationToken = default);
        
        // Method to update analysis status when review is approved
        Task UpdateAnalysisStatusAsync(
            Guid analysisId,
            RequirementsAnalysisStatus status,
            CancellationToken cancellationToken = default);

        // Get analysis by project for workflow state
        Task<RequirementsAnalysis?> GetAnalysisByProjectAsync(
            int projectId,
            CancellationToken cancellationToken = default);
    }
}
