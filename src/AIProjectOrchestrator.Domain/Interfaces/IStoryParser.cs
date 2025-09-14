using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Models.Stories;

namespace AIProjectOrchestrator.Domain.Interfaces
{
    public interface IStoryParser
    {
        Task<List<UserStory>> ParseAsync(string aiResponse, CancellationToken cancellationToken = default);
    }
}
