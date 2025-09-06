using Microsoft.EntityFrameworkCore;
using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Models.Stories;

namespace AIProjectOrchestrator.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Project> Projects => Set<Project>();
        public DbSet<RequirementsAnalysis> RequirementsAnalyses => Set<RequirementsAnalysis>();
        public DbSet<ProjectPlanning> ProjectPlannings => Set<ProjectPlanning>();
        public DbSet<StoryGeneration> StoryGenerations => Set<StoryGeneration>();
        public DbSet<PromptGeneration> PromptGenerations => Set<PromptGeneration>();
        public DbSet<Review> Reviews => Set<Review>();
        public DbSet<UserStory> UserStories => Set<UserStory>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure Project entity
            modelBuilder.Entity<Project>()
                .HasIndex(p => p.Name);

            // Configure RequirementsAnalysis entity
            modelBuilder.Entity<RequirementsAnalysis>()
                .HasIndex(ra => ra.AnalysisId)
                .IsUnique();
            
            modelBuilder.Entity<RequirementsAnalysis>()
                .HasOne(ra => ra.Project)
                .WithMany(p => p.RequirementsAnalyses)
                .HasForeignKey(ra => ra.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RequirementsAnalysis>()
                .HasOne(ra => ra.Review)
                .WithOne(r => r.RequirementsAnalysis)
                .HasForeignKey<Review>(r => r.RequirementsAnalysisId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure ProjectPlanning entity
            modelBuilder.Entity<ProjectPlanning>()
                .HasIndex(pp => pp.PlanningId)
                .IsUnique();
            
            modelBuilder.Entity<ProjectPlanning>()
                .HasOne(pp => pp.RequirementsAnalysis)
                .WithMany(ra => ra.ProjectPlannings)
                .HasForeignKey(pp => pp.RequirementsAnalysisId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProjectPlanning>()
                .HasOne(pp => pp.Review)
                .WithOne(r => r.ProjectPlanning)
                .HasForeignKey<Review>(r => r.ProjectPlanningId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure StoryGeneration entity
            modelBuilder.Entity<StoryGeneration>()
                .HasIndex(sg => sg.GenerationId)
                .IsUnique();
            
            modelBuilder.Entity<StoryGeneration>()
                .HasOne(sg => sg.ProjectPlanning)
                .WithMany(pp => pp.StoryGenerations)
                .HasForeignKey(sg => sg.ProjectPlanningId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StoryGeneration>()
                .HasOne(sg => sg.Review)
                .WithOne(r => r.StoryGeneration)
                .HasForeignKey<Review>(r => r.StoryGenerationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StoryGeneration>()
                .HasMany(sg => sg.Stories)
                .WithOne(us => us.StoryGeneration)
                .HasForeignKey(us => us.StoryGenerationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure PromptGeneration entity
            modelBuilder.Entity<PromptGeneration>()
                .HasIndex(pg => pg.PromptId)
                .IsUnique();
            
            modelBuilder.Entity<PromptGeneration>()
                .HasOne(pg => pg.StoryGeneration)
                .WithMany(sg => sg.PromptGenerations)
                .HasForeignKey(pg => pg.StoryGenerationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PromptGeneration>()
                .HasOne(pg => pg.Review)
                .WithOne(r => r.PromptGeneration)
                .HasForeignKey<Review>(r => r.PromptGenerationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Review entity indexes
            modelBuilder.Entity<Review>()
                .HasIndex(r => r.ReviewId)
                .IsUnique();
            
            modelBuilder.Entity<Review>()
                .HasIndex(r => r.ServiceName);
            
            modelBuilder.Entity<Review>()
                .HasIndex(r => r.PipelineStage);
            
            modelBuilder.Entity<Review>()
                .HasIndex(r => r.Status);

            // Add performance indexes for workflow queries
            modelBuilder.Entity<RequirementsAnalysis>()
                .HasIndex(r => r.ProjectId)
                .HasDatabaseName("IX_RequirementsAnalysis_ProjectId");

            modelBuilder.Entity<RequirementsAnalysis>()
                .HasIndex(r => r.Status)
                .HasDatabaseName("IX_RequirementsAnalysis_Status");

            modelBuilder.Entity<ProjectPlanning>()
                .HasIndex(p => p.RequirementsAnalysisId)
                .HasDatabaseName("IX_ProjectPlanning_RequirementsAnalysisId");

            modelBuilder.Entity<ProjectPlanning>()
                .HasIndex(p => p.Status)
                .HasDatabaseName("IX_ProjectPlanning_Status");

            modelBuilder.Entity<StoryGeneration>()
                .HasIndex(s => s.ProjectPlanningId)
                .HasDatabaseName("IX_StoryGeneration_PlanningId");

            modelBuilder.Entity<StoryGeneration>()
                .HasIndex(s => s.Status)
                .HasDatabaseName("IX_StoryGeneration_Status");

            modelBuilder.Entity<PromptGeneration>()
                .HasIndex(p => p.StoryGenerationId)
                .HasDatabaseName("IX_PromptGeneration_StoryGenerationId");

            modelBuilder.Entity<PromptGeneration>()
                .HasIndex(p => p.Status)
                .HasDatabaseName("IX_PromptGeneration_Status");

            modelBuilder.Entity<Review>()
                .HasIndex(r => new { r.PipelineStage, r.Status })
                .HasDatabaseName("IX_Review_PipelineStage_Status");

            modelBuilder.Entity<Review>()
                .HasIndex(r => r.CreatedDate)
                .HasDatabaseName("IX_Review_CreatedDate");
        }
    }
}
