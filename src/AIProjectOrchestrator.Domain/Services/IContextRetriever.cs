using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Models;

namespace AIProjectOrchestrator.Domain.Services;

public interface IContextRetriever
{
    Task<ComprehensiveContext> RetrieveComprehensiveContextAsync(Guid storyGenerationId, CancellationToken cancellationToken = default);

    List<UserStory> OptimizeStoriesContext(List<UserStory> stories);

    string OptimizeTechnicalContext(string technicalContext);

    string OptimizeBusinessContext(string businessContext);
}