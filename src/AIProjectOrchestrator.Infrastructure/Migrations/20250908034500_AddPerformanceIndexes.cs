using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIProjectOrchestrator.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Index for ProjectPlanning table - common queries by ProjectId and Status
            migrationBuilder.CreateIndex(
                name: "IX_ProjectPlannings_ProjectId_Status",
                table: "ProjectPlannings",
                columns: new[] { "ProjectId", "Status" });

            // Index for StoryGeneration table - queries by ProjectPlanningId and Status
            migrationBuilder.CreateIndex(
                name: "IX_StoryGenerations_ProjectPlanningId_Status",
                table: "StoryGenerations",
                columns: new[] { "ProjectPlanningId", "Status" });

            // Index for Review table - queries by Status (Pending reviews) and ServiceName
            migrationBuilder.CreateIndex(
                name: "IX_Reviews_Status_ServiceName",
                table: "Reviews",
                columns: new[] { "Status", "ServiceName" });

            // Composite index for Review by PipelineStage and ProjectId
            migrationBuilder.CreateIndex(
                name: "IX_Reviews_PipelineStage_ProjectId",
                table: "Reviews",
                columns: new[] { "PipelineStage", "ProjectId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProjectPlannings_ProjectId_Status",
                table: "ProjectPlannings");

            migrationBuilder.DropIndex(
                name: "IX_StoryGenerations_ProjectPlanningId_Status",
                table: "StoryGenerations");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_Status_ServiceName",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_PipelineStage_ProjectId",
                table: "Reviews");
        }
    }
}