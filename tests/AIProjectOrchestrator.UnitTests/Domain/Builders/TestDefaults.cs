using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Models;
using AIProjectOrchestrator.Domain.Models.PromptGeneration;
using AIProjectOrchestrator.Domain.Models.Review;
using AIProjectOrchestrator.Domain.Models.Stories;
using System;

namespace AIProjectOrchestrator.UnitTests.Domain.Builders
{
    public static class TestDefaults
    {
        public static string EmptyString => string.Empty;
        public static Guid EmptyGuid => Guid.Empty;
        public static List<T> EmptyList<T>() => new List<T>();
        
        public static Project DefaultProject() => new Project
        {
            Id = 1,
            Name = "Default Test Project",
            Description = "Default Test Description",
            Status = "active",
            Type = "web",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow,
            RequirementsAnalyses = EmptyList<RequirementsAnalysis>()
        };
        
        public static RequirementsAnalysis DefaultRequirementsAnalysis() => new RequirementsAnalysis
        {
            Id = 1,
            ProjectId = 1,
            AnalysisId = "analysis-1",
            Status = RequirementsAnalysisStatus.NotStarted,
            Content = "Default Requirements Analysis Content",
            ReviewId = "review-1",
            CreatedDate = DateTime.UtcNow,
            Project = DefaultProject(),
            Review = DefaultReview(),
            ProjectPlannings = EmptyList<ProjectPlanning>()
        };
        
        public static ProjectPlanning DefaultProjectPlanning() => new ProjectPlanning
        {
            Id = 1,
            RequirementsAnalysisId = 1,
            PlanningId = "planning-1",
            Status = ProjectPlanningStatus.NotStarted,
            Content = "Default Project Planning Content",
            ReviewId = "review-1",
            CreatedDate = DateTime.UtcNow,
            RequirementsAnalysis = DefaultRequirementsAnalysis(),
            Review = DefaultReview(),
            StoryGenerations = EmptyList<StoryGeneration>()
        };
        
        public static StoryGeneration DefaultStoryGeneration() => new StoryGeneration
        {
            Id = 1,
            ProjectPlanningId = 1,
            GenerationId = "generation-1",
            Status = StoryGenerationStatus.NotStarted,
            Content = "Default Story Generation Content",
            ReviewId = "review-1",
            CreatedDate = DateTime.UtcNow,
            StoriesJson = "[]",
            ProjectPlanning = DefaultProjectPlanning(),
            Review = DefaultReview(),
            PromptGenerations = EmptyList<PromptGeneration>(),
            Stories = EmptyList<UserStory>()
        };
        
        public static UserStory DefaultUserStory() => new UserStory
        {
            Id = Guid.NewGuid(),
            StoryGenerationId = 1,
            Title = "Default User Story",
            Description = "Default Description",
            AcceptanceCriteria = EmptyList<string>(),
            Priority = "Medium",
            StoryPoints = 3,
            Tags = EmptyList<string>(),
            EstimatedComplexity = "Medium",
            Status = StoryStatus.Draft,
            HasPrompt = false,
            PromptId = null,
            StoryGeneration = DefaultStoryGeneration(),
            PromptGenerations = EmptyList<PromptGeneration>()
        };
        
        public static PromptGeneration DefaultPromptGeneration() => new PromptGeneration
        {
            Id = 1,
            UserStoryId = Guid.NewGuid(),
            StoryIndex = 0,
            PromptId = "prompt-1",
            Status = PromptGenerationStatus.NotStarted,
            Content = "Default Prompt Content",
            ReviewId = "review-1",
            CreatedDate = DateTime.UtcNow,
            UserStory = DefaultUserStory(),
            Review = DefaultReview()
        };
        
        public static PromptTemplate DefaultPromptTemplate() => new PromptTemplate
        {
            Id = Guid.NewGuid(),
            Title = "Default Template",
            Content = "Default Template Content",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        public static AIProjectOrchestrator.Domain.Entities.Review DefaultReview() => new AIProjectOrchestrator.Domain.Entities.Review
        {
            Id = 1,
            ReviewId = Guid.NewGuid(),
            Content = "Default Review Content",
            Status = ReviewStatus.Pending,
            ServiceName = "TestService",
            PipelineStage = "TestStage",
            Feedback = "Default Feedback",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow,
            RequirementsAnalysisId = null,
            ProjectPlanningId = null,
            StoryGenerationId = null,
            PromptGenerationId = null,
            RequirementsAnalysis = DefaultRequirementsAnalysis(),
            ProjectPlanning = DefaultProjectPlanning(),
            StoryGeneration = DefaultStoryGeneration(),
            PromptGeneration = DefaultPromptGeneration()
        };
    }
}