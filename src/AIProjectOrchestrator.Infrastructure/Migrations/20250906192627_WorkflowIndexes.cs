using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIProjectOrchestrator.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class WorkflowIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_StoryGenerations_ProjectPlanningId",
                table: "StoryGenerations",
                newName: "IX_StoryGeneration_PlanningId");

            migrationBuilder.RenameIndex(
                name: "IX_RequirementsAnalyses_ProjectId",
                table: "RequirementsAnalyses",
                newName: "IX_RequirementsAnalysis_ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_PromptGenerations_StoryGenerationId",
                table: "PromptGenerations",
                newName: "IX_PromptGeneration_StoryGenerationId");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectPlannings_RequirementsAnalysisId",
                table: "ProjectPlannings",
                newName: "IX_ProjectPlanning_RequirementsAnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_StoryGeneration_Status",
                table: "StoryGenerations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Review_CreatedDate",
                table: "Reviews",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_Review_PipelineStage_Status",
                table: "Reviews",
                columns: new[] { "PipelineStage", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RequirementsAnalysis_Status",
                table: "RequirementsAnalyses",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PromptGeneration_Status",
                table: "PromptGenerations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPlanning_Status",
                table: "ProjectPlannings",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StoryGeneration_Status",
                table: "StoryGenerations");

            migrationBuilder.DropIndex(
                name: "IX_Review_CreatedDate",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Review_PipelineStage_Status",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_RequirementsAnalysis_Status",
                table: "RequirementsAnalyses");

            migrationBuilder.DropIndex(
                name: "IX_PromptGeneration_Status",
                table: "PromptGenerations");

            migrationBuilder.DropIndex(
                name: "IX_ProjectPlanning_Status",
                table: "ProjectPlannings");

            migrationBuilder.RenameIndex(
                name: "IX_StoryGeneration_PlanningId",
                table: "StoryGenerations",
                newName: "IX_StoryGenerations_ProjectPlanningId");

            migrationBuilder.RenameIndex(
                name: "IX_RequirementsAnalysis_ProjectId",
                table: "RequirementsAnalyses",
                newName: "IX_RequirementsAnalyses_ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_PromptGeneration_StoryGenerationId",
                table: "PromptGenerations",
                newName: "IX_PromptGenerations_StoryGenerationId");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectPlanning_RequirementsAnalysisId",
                table: "ProjectPlannings",
                newName: "IX_ProjectPlannings_RequirementsAnalysisId");
        }
    }
}
