using System;
using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Models.Stories;

namespace AIProjectOrchestrator.Domain.Interfaces
{
    public interface IStoryDependencyValidator
    {
        Task ValidateAsync(Guid planningId, CancellationToken cancellationToken = default);
    }
}
