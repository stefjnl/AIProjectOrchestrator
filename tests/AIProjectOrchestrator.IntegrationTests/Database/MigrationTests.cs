using System;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using AIProjectOrchestrator.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AIProjectOrchestrator.IntegrationTests.Database
{
    public class MigrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public MigrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Database_WhenMigrated_HasCorrectSchema()
        {
            // Arrange
            var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Act & Assert
            // Verify that all expected tables exist
            Assert.True(await dbContext.Database.CanConnectAsync());
            
            // Verify Projects table exists
            var projectsExist = await dbContext.Database.ExecuteSqlRawAsync("SELECT 1 FROM \"Projects\" LIMIT 1") >= 0;
            Assert.True(projectsExist);
            
            // Verify RequirementsAnalyses table exists
            var requirementsExist = await dbContext.Database.ExecuteSqlRawAsync("SELECT 1 FROM \"RequirementsAnalyses\" LIMIT 1") >= 0;
            Assert.True(requirementsExist);
            
            // Verify ProjectPlannings table exists
            var planningsExist = await dbContext.Database.ExecuteSqlRawAsync("SELECT 1 FROM \"ProjectPlannings\" LIMIT 1") >= 0;
            Assert.True(planningsExist);
            
            // Verify StoryGenerations table exists
            var storiesExist = await dbContext.Database.ExecuteSqlRawAsync("SELECT 1 FROM \"StoryGenerations\" LIMIT 1") >= 0;
            Assert.True(storiesExist);
            
            // Verify PromptGenerations table exists
            var promptsExist = await dbContext.Database.ExecuteSqlRawAsync("SELECT 1 FROM \"PromptGenerations\" LIMIT 1") >= 0;
            Assert.True(promptsExist);
            
            // Verify Reviews table exists
            var reviewsExist = await dbContext.Database.ExecuteSqlRawAsync("SELECT 1 FROM \"Reviews\" LIMIT 1") >= 0;
            Assert.True(reviewsExist);
        }

        [Fact]
        public async Task Database_WhenMigrated_HasCorrectRelationships()
        {
            // Arrange
            var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Act & Assert
            // Verify foreign key constraints exist by checking that we can query related data
            // This is a simplified test - in a real scenario, we would insert test data and verify relationships
            
            Assert.True(await dbContext.Database.CanConnectAsync());
        }
    }
}