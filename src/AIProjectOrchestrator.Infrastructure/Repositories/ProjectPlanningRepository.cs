using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Interfaces;
using AIProjectOrchestrator.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AIProjectOrchestrator.Infrastructure.Repositories
{
    public class ProjectPlanningRepository : Repository<ProjectPlanning>, IProjectPlanningRepository
    {
        public ProjectPlanningRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<ProjectPlanning?> GetByPlanningIdAsync(string planningId, CancellationToken cancellationToken = default)
        {
            return await _context.ProjectPlannings
                .FirstOrDefaultAsync(pp => pp.PlanningId == planningId, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<ProjectPlanning?> GetByRequirementsAnalysisIdAsync(int requirementsAnalysisId, CancellationToken cancellationToken = default)
        {
            return await _context.ProjectPlannings
                .FirstOrDefaultAsync(pp => pp.RequirementsAnalysisId == requirementsAnalysisId, cancellationToken)
                .ConfigureAwait(false);
        }

        public new async Task<ProjectPlanning?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.ProjectPlannings
                .FirstOrDefaultAsync(pp => pp.Id == id, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<ProjectPlanning?> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default)
        {
            return await _context.ProjectPlannings
                .FirstOrDefaultAsync(pp => pp.RequirementsAnalysis.ProjectId == projectId, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
