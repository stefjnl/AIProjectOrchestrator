using Microsoft.EntityFrameworkCore;
using AIProjectOrchestrator.Domain.Entities;

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
        }
    }
}
