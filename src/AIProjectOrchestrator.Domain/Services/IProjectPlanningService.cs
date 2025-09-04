using System;
using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Models;

namespace AIProjectOrchestrator.Domain.Services
{
    public interface IProjectPlanningService
    {
        Task<ProjectPlanningResponse> CreateProjectPlanAsync(
            ProjectPlanningRequest request,
            CancellationToken cancellationToken = default);

        Task<ProjectPlanningStatus> GetPlanningStatusAsync(
            Guid planningId,
            CancellationToken cancellationToken = default);

        Task<bool> CanCreatePlanAsync(
            Guid requirementsAnalysisId,
            CancellationToken cancellationToken = default);

        Task<string?> GetPlanningResultContentAsync(
            Guid planningId,
            CancellationToken cancellationToken = default);

        Task<Guid?> GetRequirementsAnalysisIdAsync(
            Guid planningId,
            CancellationToken cancellationToken = default);
            
        Task<string?> GetTechnicalContextAsync(Guid planningId, CancellationToken cancellationToken = default);
        
        // Method to update planning status when review is approved
        Task UpdatePlanningStatusAsync(
            Guid planningId,
            ProjectPlanningStatus status,
            CancellationToken cancellationToken = default);
    }
}
