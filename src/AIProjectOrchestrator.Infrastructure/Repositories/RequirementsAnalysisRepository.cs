using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Interfaces;
using AIProjectOrchestrator.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AIProjectOrchestrator.Infrastructure.Repositories
{
    public class RequirementsAnalysisRepository : Repository<RequirementsAnalysis>, IRequirementsAnalysisRepository
    {
        public RequirementsAnalysisRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<RequirementsAnalysis?> GetByAnalysisIdAsync(string analysisId, CancellationToken cancellationToken = default)
        {
            return await _context.RequirementsAnalyses
                .FirstOrDefaultAsync(ra => ra.AnalysisId == analysisId, cancellationToken);
        }

        public async Task<RequirementsAnalysis?> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default)
        {
            return await _context.RequirementsAnalyses
                .FirstOrDefaultAsync(ra => ra.ProjectId == projectId, cancellationToken);
        }

        public new async Task<RequirementsAnalysis?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.RequirementsAnalyses
                .FirstOrDefaultAsync(ra => ra.Id == id, cancellationToken);
        }

        public async Task<int?> GetEntityIdByAnalysisIdAsync(string analysisId, CancellationToken cancellationToken = default)
        {
            var entity = await _context.RequirementsAnalyses
                .FirstOrDefaultAsync(ra => ra.AnalysisId == analysisId, cancellationToken);
            return entity?.Id;
        }
    }
}
