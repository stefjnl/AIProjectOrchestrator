using System;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using AIProjectOrchestrator.Domain.Interfaces;
using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Models;
using AIProjectOrchestrator.Domain.Models.Stories;
using AIProjectOrchestrator.Domain.Models.PromptGeneration;
using AIProjectOrchestrator.Domain.Models.Review;
using System.Collections.Generic;
using System.Linq;

namespace AIProjectOrchestrator.IntegrationTests.Database
{
    public class DatabasePersistenceTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public DatabasePersistenceTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task StoryGeneration_WhenSavedToDatabase_CanBeRetrieved()
        {
            // Arrange
            var scope = _factory.Services.CreateScope();
            var projectPlanningRepository = scope.ServiceProvider.GetRequiredService<IProjectPlanningRepository>();
            var storyGenerationRepository = scope.ServiceProvider.GetRequiredService<IStoryGenerationRepository>();

            var project = new Project
            {
                Name = "Test Project",
                Description = "Test project for persistence testing",
                CreatedDate = DateTime.UtcNow
            };

            var requirementsAnalysis = new AIProjectOrchestrator.Domain.Entities.RequirementsAnalysis
            {
                AnalysisId = Guid.NewGuid().ToString(),
                Status = RequirementsAnalysisStatus.Approved,
                Content = "Test requirements content",
                ReviewId = Guid.NewGuid().ToString(),
                CreatedDate = DateTime.UtcNow
            };

            var projectPlanning = new AIProjectOrchestrator.Domain.Entities.ProjectPlanning
            {
                PlanningId = Guid.NewGuid().ToString(),
                Status = ProjectPlanningStatus.Approved,
                Content = "Test planning content",
                ReviewId = Guid.NewGuid().ToString(),
                CreatedDate = DateTime.UtcNow
            };

            var generationId = Guid.NewGuid().ToString();
            var stories = new List<UserStory>
            {
                new UserStory
                {
                    Title = "Test Story",
                    Description = "Test Description",
                    AcceptanceCriteria = new List<string> { "Criteria 1", "Criteria 2" },
                    Priority = "High"
                }
            };

            var storyGeneration = new StoryGeneration
            {
                GenerationId = generationId,
                Status = Domain.Models.Stories.StoryGenerationStatus.Approved,
                Content = "Test content",
                ReviewId = Guid.NewGuid().ToString(),
                CreatedDate = DateTime.UtcNow,
                Stories = stories
            };

            // Act
            var projectRepository = scope.ServiceProvider.GetRequiredService<IProjectRepository>();
            var requirementsAnalysisRepository = scope.ServiceProvider.GetRequiredService<IRequirementsAnalysisRepository>();
            // Reuse projectPlanningRepository from Arrange
            // Reuse storyGenerationRepository from Arrange
            await projectRepository.AddAsync(project);
            requirementsAnalysis.ProjectId = project.Id;
            await requirementsAnalysisRepository.AddAsync(requirementsAnalysis);
            projectPlanning.RequirementsAnalysisId = requirementsAnalysis.Id;
            await projectPlanningRepository.AddAsync(projectPlanning);
            storyGeneration.ProjectPlanningId = projectPlanning.Id;
            await storyGenerationRepository.AddAsync(storyGeneration);

            // Retrieve the story generation
            var retrievedStoryGeneration = await storyGenerationRepository.GetByGenerationIdAsync(generationId);

            // Assert
            Assert.NotNull(retrievedStoryGeneration);
            Assert.Equal(storyGeneration.GenerationId, retrievedStoryGeneration.GenerationId);
            Assert.Equal(storyGeneration.Status, retrievedStoryGeneration.Status);
            Assert.Equal(storyGeneration.Content, retrievedStoryGeneration.Content);
            Assert.Equal(storyGeneration.ReviewId, retrievedStoryGeneration.ReviewId);
            Assert.Equal(storyGeneration.Stories.Count, retrievedStoryGeneration.Stories.Count);
            Assert.Equal(storyGeneration.Stories.First().Title, retrievedStoryGeneration.Stories.First().Title);
        }

        [Fact]
        public async Task PromptGeneration_WhenSavedToDatabase_CanBeRetrieved()
        {
            // Arrange
            var scope = _factory.Services.CreateScope();
            var projectPlanningRepository = scope.ServiceProvider.GetRequiredService<IProjectPlanningRepository>();
            var storyGenerationRepository = scope.ServiceProvider.GetRequiredService<IStoryGenerationRepository>();
            var promptGenerationRepository = scope.ServiceProvider.GetRequiredService<IPromptGenerationRepository>();

            var project = new Project
            {
                Name = "Test Project",
                Description = "Test project for persistence testing",
                CreatedDate = DateTime.UtcNow
            };

            var requirementsAnalysis = new AIProjectOrchestrator.Domain.Entities.RequirementsAnalysis
            {
                AnalysisId = Guid.NewGuid().ToString(),
                Status = RequirementsAnalysisStatus.Approved,
                Content = "Test requirements content",
                ReviewId = Guid.NewGuid().ToString(),
                CreatedDate = DateTime.UtcNow
            };

            var projectPlanning = new AIProjectOrchestrator.Domain.Entities.ProjectPlanning
            {
                PlanningId = Guid.NewGuid().ToString(),
                Status = ProjectPlanningStatus.Approved,
                Content = "Test planning content",
                ReviewId = Guid.NewGuid().ToString(),
                CreatedDate = DateTime.UtcNow
            };

            var generationId = Guid.NewGuid().ToString();
            var stories = new List<UserStory>
            {
                new UserStory
                {
                    Title = "Test Story",
                    Description = "Test Description",
                    AcceptanceCriteria = new List<string> { "Criteria 1", "Criteria 2" },
                    Priority = "High"
                }
            };

            var storyGeneration = new StoryGeneration
            {
                GenerationId = generationId,
                Status = Domain.Models.Stories.StoryGenerationStatus.Approved,
                Content = "Test content",
                ReviewId = Guid.NewGuid().ToString(),
                CreatedDate = DateTime.UtcNow,
                Stories = stories
            };

            var promptId = Guid.NewGuid().ToString();
            var promptGeneration = new PromptGeneration
            {
                PromptId = promptId,
                StoryIndex = 0,
                Status = Domain.Models.PromptGeneration.PromptGenerationStatus.Approved,
                Content = "Test prompt content",
                ReviewId = Guid.NewGuid().ToString(),
                CreatedDate = DateTime.UtcNow
            };

            // Act
            var projectRepository = scope.ServiceProvider.GetRequiredService<IProjectRepository>();
            var requirementsAnalysisRepository = scope.ServiceProvider.GetRequiredService<IRequirementsAnalysisRepository>();
            // Reuse projectPlanningRepository from Arrange
            // Reuse storyGenerationRepository from Arrange
            // Reuse promptGenerationRepository from Arrange
            await projectRepository.AddAsync(project);
            requirementsAnalysis.ProjectId = project.Id;
            await requirementsAnalysisRepository.AddAsync(requirementsAnalysis);
            projectPlanning.RequirementsAnalysisId = requirementsAnalysis.Id;
            await projectPlanningRepository.AddAsync(projectPlanning);
            storyGeneration.ProjectPlanningId = projectPlanning.Id;
            await storyGenerationRepository.AddAsync(storyGeneration);
            promptGeneration.StoryGenerationId = storyGeneration.Id;
            await promptGenerationRepository.AddAsync(promptGeneration);

            // Retrieve the prompt generation
            var retrievedPromptGeneration = await promptGenerationRepository.GetByPromptIdAsync(promptId);

            // Assert
            Assert.NotNull(retrievedPromptGeneration);
            Assert.Equal(promptGeneration.PromptId, retrievedPromptGeneration.PromptId);
            Assert.Equal(promptGeneration.Status, retrievedPromptGeneration.Status);
            Assert.Equal(promptGeneration.Content, retrievedPromptGeneration.Content);
            Assert.Equal(promptGeneration.ReviewId, retrievedPromptGeneration.ReviewId);
            Assert.Equal(promptGeneration.StoryIndex, retrievedPromptGeneration.StoryIndex);
        }

        [Fact]
        public async Task ReviewEntity_WhenSavedToDatabase_CanBeRetrieved()
        {
            // Arrange
            var reviewId = Guid.NewGuid();
            var review = new Domain.Entities.Review
            {
                ReviewId = reviewId,
                Content = "Test review content",
                Status = ReviewStatus.Approved,
                ServiceName = "TestService",
                PipelineStage = "TestStage",
                Feedback = "Test feedback",
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };

            // Act
            var scope = _factory.Services.CreateScope();
            var reviewRepository = scope.ServiceProvider.GetRequiredService<IReviewRepository>();

            // Save the review
            await reviewRepository.AddAsync(review);

            // Retrieve the review
            var retrievedReview = await reviewRepository.GetByReviewIdAsync(reviewId);

            // Assert
            Assert.NotNull(retrievedReview);
            Assert.Equal(review.ReviewId, retrievedReview.ReviewId);
            Assert.Equal(review.Status, retrievedReview.Status);
            Assert.Equal(review.Content, retrievedReview.Content);
            Assert.Equal(review.ServiceName, retrievedReview.ServiceName);
            Assert.Equal(review.PipelineStage, retrievedReview.PipelineStage);
            Assert.Equal(review.Feedback, retrievedReview.Feedback);
        }
    }
}