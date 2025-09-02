using AIProjectOrchestrator.Domain.Models;
using System.Threading.Tasks;
using System.Threading;

namespace AIProjectOrchestrator.Domain.Services
{
    public interface IInstructionService
    {
        Task<InstructionContent> GetInstructionAsync(string serviceName, CancellationToken cancellationToken = default);
        Task<bool> IsValidInstructionAsync(string serviceName, CancellationToken cancellationToken = default);
    }
}