using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Models;
using AIProjectOrchestrator.Domain.Models.Stories;
using AIProjectOrchestrator.Domain.Models.Review;
using AIProjectOrchestrator.Domain.Models.PromptGeneration;

namespace AIProjectOrchestrator.UnitTests.Domain.Builders
{
    public static class EntityBuilders
    {
        public static Project BuildProject(
            int id = 0,
            string name = "Test Project",
            string description = "Test Description",
            string status = "active",
            string type = "web",
            DateTime? createdDate = null,
            DateTime? updatedDate = null)
        {
            return new Project
            {
                Id = id,
                Name = name,
                Description = description,
                Status = status,
                Type = type,
                CreatedDate = createdDate ?? DateTime.UtcNow,
                UpdatedDate = updatedDate ?? DateTime.UtcNow,
                RequirementsAnalyses = new List<RequirementsAnalysis>()
            };
        }

        public static RequirementsAnalysis BuildRequirementsAnalysis(
            int id = 0,
            int projectId = 1,
            string analysisId = "analysis-1",
            AIProjectOrchestrator.Domain.Models.RequirementsAnalysisStatus status = AIProjectOrchestrator.Domain.Models.RequirementsAnalysisStatus.NotStarted,
            string content = "Test Requirements Analysis Content",
            string reviewId = "review-1",
            DateTime? createdDate = null)
        {
            return new RequirementsAnalysis
            {
                Id = id,
                ProjectId = projectId,
                AnalysisId = analysisId,
                Status = status,
                Content = content,
                ReviewId = reviewId,
                CreatedDate = createdDate ?? DateTime.UtcNow,
                Project = TestDefaults.DefaultProject(),
                Review = TestDefaults.DefaultReview(),
                ProjectPlannings = new List<ProjectPlanning>()
            };
        }

        public static ProjectPlanning BuildProjectPlanning(
            int id = 0,
            int requirementsAnalysisId = 1,
            string planningId = "planning-1",
            AIProjectOrchestrator.Domain.Models.ProjectPlanningStatus status = AIProjectOrchestrator.Domain.Models.ProjectPlanningStatus.NotStarted,
            string content = "Test Project Planning Content",
            string reviewId = "review-1",
            DateTime? createdDate = null)
        {
            return new ProjectPlanning
            {
                Id = id,
                RequirementsAnalysisId = requirementsAnalysisId,
                PlanningId = planningId,
                Status = status,
                Content = content,
                ReviewId = reviewId,
                CreatedDate = createdDate ?? DateTime.UtcNow,
                RequirementsAnalysis = TestDefaults.DefaultRequirementsAnalysis(),
                Review = TestDefaults.DefaultReview(),
                StoryGenerations = new List<StoryGeneration>()
            };
        }

        public static StoryGeneration BuildStoryGeneration(
            int id = 0,
            int projectPlanningId = 1,
            string generationId = "generation-1",
            AIProjectOrchestrator.Domain.Models.Stories.StoryGenerationStatus status = AIProjectOrchestrator.Domain.Models.Stories.StoryGenerationStatus.NotStarted,
            string content = "Test Story Generation Content",
            string reviewId = "review-1",
            string storiesJson = "[]",
            DateTime? createdDate = null)
        {
            return new StoryGeneration
            {
                Id = id,
                ProjectPlanningId = projectPlanningId,
                GenerationId = generationId,
                Status = status,
                Content = content,
                ReviewId = reviewId,
                CreatedDate = createdDate ?? DateTime.UtcNow,
                StoriesJson = storiesJson,
                ProjectPlanning = TestDefaults.DefaultProjectPlanning(),
                Review = TestDefaults.DefaultReview(),
                PromptGenerations = new List<PromptGeneration>(),
                Stories = new List<UserStory>()
            };
        }

        public static UserStory BuildUserStory(
            Guid? id = null,
            int storyGenerationId = 1,
            string title = "Test User Story",
            string description = "Test Description",
            List<string>? acceptanceCriteria = null,
            string priority = "Medium",
            int? storyPoints = 3,
            List<string>? tags = null,
            string? estimatedComplexity = "Medium",
            StoryStatus status = StoryStatus.Draft,
            bool hasPrompt = false,
            string? promptId = null)
        {
            return new UserStory
            {
                Id = id ?? Guid.NewGuid(),
                StoryGenerationId = storyGenerationId,
                Title = title,
                Description = description,
                AcceptanceCriteria = acceptanceCriteria ?? new List<string>(),
                Priority = priority,
                StoryPoints = storyPoints,
                Tags = tags ?? new List<string>(),
                EstimatedComplexity = estimatedComplexity,
                Status = status,
                HasPrompt = hasPrompt,
                PromptId = promptId,
                StoryGeneration = TestDefaults.DefaultStoryGeneration(),
                PromptGenerations = new List<PromptGeneration>()
            };
        }

        public static PromptGeneration BuildPromptGeneration(
            int id = 0,
            Guid userStoryId = new Guid(),
            int storyIndex = 0,
            string promptId = "prompt-1",
            AIProjectOrchestrator.Domain.Models.PromptGeneration.PromptGenerationStatus status = AIProjectOrchestrator.Domain.Models.PromptGeneration.PromptGenerationStatus.NotStarted,
            string content = "Test Prompt Content",
            string reviewId = "review-1",
            DateTime? createdDate = null)
        {
            // Generate a proper Guid if default value is used
            var actualUserStoryId = userStoryId == Guid.Empty ? Guid.NewGuid() : userStoryId;
            
            return new PromptGeneration
            {
                Id = id,
                UserStoryId = actualUserStoryId,
                StoryIndex = storyIndex,
                PromptId = promptId,
                Status = status,
                Content = content,
                ReviewId = reviewId,
                CreatedDate = createdDate ?? DateTime.UtcNow,
                UserStory = TestDefaults.DefaultUserStory(),
                Review = TestDefaults.DefaultReview()
            };
        }

        public static PromptTemplate BuildPromptTemplate(
            Guid? id = null,
            string title = "Test Template",
            string content = "Test Template Content",
            DateTime? createdAt = null,
            DateTime? updatedAt = null)
        {
            return new PromptTemplate
            {
                Id = id ?? Guid.NewGuid(),
                Title = title,
                Content = content,
                CreatedAt = createdAt ?? DateTime.UtcNow,
                UpdatedAt = updatedAt ?? DateTime.UtcNow
            };
        }

        public static AIProjectOrchestrator.Domain.Entities.Review BuildReview(
            int id = 0,
            Guid? reviewId = null,
            string content = "Test Review Content",
            AIProjectOrchestrator.Domain.Models.Review.ReviewStatus status = AIProjectOrchestrator.Domain.Models.Review.ReviewStatus.Pending,
            string serviceName = "TestService",
            string pipelineStage = "TestStage",
            string feedback = "Test Feedback",
            DateTime? createdDate = null,
            DateTime? updatedDate = null,
            int? requirementsAnalysisId = null,
            int? projectPlanningId = null,
            int? storyGenerationId = null,
            int? promptGenerationId = null)
        {
            return new AIProjectOrchestrator.Domain.Entities.Review
            {
                Id = id,
                ReviewId = reviewId ?? Guid.NewGuid(),
                Content = content,
                Status = status,
                ServiceName = serviceName,
                PipelineStage = pipelineStage,
                Feedback = feedback,
                CreatedDate = createdDate ?? DateTime.UtcNow,
                UpdatedDate = updatedDate ?? DateTime.UtcNow,
                RequirementsAnalysisId = requirementsAnalysisId,
                ProjectPlanningId = projectPlanningId,
                StoryGenerationId = storyGenerationId,
                PromptGenerationId = promptGenerationId,
                RequirementsAnalysis = TestDefaults.DefaultRequirementsAnalysis(),
                ProjectPlanning = TestDefaults.DefaultProjectPlanning(),
                StoryGeneration = TestDefaults.DefaultStoryGeneration(),
                PromptGeneration = TestDefaults.DefaultPromptGeneration()
            };
        }
    }
}