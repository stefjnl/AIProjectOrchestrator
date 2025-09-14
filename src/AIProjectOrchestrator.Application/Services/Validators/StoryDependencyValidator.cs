using System;
using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.Application.Interfaces;
using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Exceptions;
using AIProjectOrchestrator.Domain.Interfaces;
using AIProjectOrchestrator.Domain.Models;
using AIProjectOrchestrator.Domain.Services;
using Microsoft.Extensions.Logging;

namespace AIProjectOrchestrator.Application.Services.Validators
{
    public class StoryDependencyValidator : IStoryDependencyValidator
    {
        private readonly IProjectPlanningService _projectPlanningService;
        private readonly IRequirementsAnalysisService _requirementsAnalysisService;
        private readonly ILogger<StoryDependencyValidator> _logger;

        public StoryDependencyValidator(
            IProjectPlanningService projectPlanningService,
            IRequirementsAnalysisService requirementsAnalysisService,
            ILogger<StoryDependencyValidator> logger)
        {
            _projectPlanningService = projectPlanningService;
            _requirementsAnalysisService = requirementsAnalysisService;
            _logger = logger;
        }

        public async Task ValidateAsync(Guid planningId, CancellationToken cancellationToken = default)
        {
            // Validate input
            if (planningId == Guid.Empty)
            {
                _logger.LogWarning("Story dependency validation failed: Planning ID is required");
                throw new ArgumentException("Planning ID is required");
            }

            // Get the requirements analysis ID from the planning ID
            var requirementsAnalysisId = await _projectPlanningService.GetRequirementsAnalysisIdAsync(
                planningId, cancellationToken);

            if (requirementsAnalysisId == null)
            {
                _logger.LogWarning("Story dependency validation failed: Planning {PlanningId} not found", planningId);
                throw new InvalidOperationException("Planning not found");
            }

            // Check that requirements analysis is approved
            var requirementsAnalysis = await _requirementsAnalysisService.GetAnalysisResultsAsync(
                requirementsAnalysisId.Value, cancellationToken);
            if (requirementsAnalysis == null)
            {
                _logger.LogWarning("Story dependency validation failed: Requirements analysis {RequirementsAnalysisId} not found",
                    requirementsAnalysisId.Value);
                throw new InvalidOperationException("Requirements analysis not found");
            }

            if (requirementsAnalysis.Status != RequirementsAnalysisStatus.Approved)
            {
                _logger.LogWarning("Story dependency validation failed: Requirements analysis {RequirementsAnalysisId} is not approved (status: {Status})",
                    requirementsAnalysisId.Value, requirementsAnalysis.Status);
                throw new InvalidOperationException("Requirements analysis is not approved");
            }

            // Check that project planning is approved
            var planningStatus = await _projectPlanningService.GetPlanningStatusAsync(planningId, cancellationToken);
            if (planningStatus != ProjectPlanningStatus.Approved)
            {
                _logger.LogWarning("Story dependency validation failed: Planning {PlanningId} is not approved (status: {Status})",
                    planningId, planningStatus);
                throw new InvalidOperationException("Planning is not approved");
            }

            _logger.LogDebug("Story dependency validation succeeded for planning {PlanningId}", planningId);
        }
    }
}
