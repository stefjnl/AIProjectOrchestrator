using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AIProjectOrchestrator.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixReviewRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_ProjectPlannings_Id",
                table: "Reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_PromptGenerations_Id",
                table: "Reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_RequirementsAnalyses_Id",
                table: "Reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_StoryGenerations_Id",
                table: "Reviews");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Reviews",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<int>(
                name: "ProjectPlanningId",
                table: "Reviews",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PromptGenerationId",
                table: "Reviews",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequirementsAnalysisId",
                table: "Reviews",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StoryGenerationId",
                table: "Reviews",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ProjectPlanningId",
                table: "Reviews",
                column: "ProjectPlanningId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_PromptGenerationId",
                table: "Reviews",
                column: "PromptGenerationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_RequirementsAnalysisId",
                table: "Reviews",
                column: "RequirementsAnalysisId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_StoryGenerationId",
                table: "Reviews",
                column: "StoryGenerationId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_ProjectPlannings_ProjectPlanningId",
                table: "Reviews",
                column: "ProjectPlanningId",
                principalTable: "ProjectPlannings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_PromptGenerations_PromptGenerationId",
                table: "Reviews",
                column: "PromptGenerationId",
                principalTable: "PromptGenerations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_RequirementsAnalyses_RequirementsAnalysisId",
                table: "Reviews",
                column: "RequirementsAnalysisId",
                principalTable: "RequirementsAnalyses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_StoryGenerations_StoryGenerationId",
                table: "Reviews",
                column: "StoryGenerationId",
                principalTable: "StoryGenerations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_ProjectPlannings_ProjectPlanningId",
                table: "Reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_PromptGenerations_PromptGenerationId",
                table: "Reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_RequirementsAnalyses_RequirementsAnalysisId",
                table: "Reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_StoryGenerations_StoryGenerationId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_ProjectPlanningId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_PromptGenerationId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_RequirementsAnalysisId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_StoryGenerationId",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "ProjectPlanningId",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "PromptGenerationId",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "RequirementsAnalysisId",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "StoryGenerationId",
                table: "Reviews");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Reviews",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_ProjectPlannings_Id",
                table: "Reviews",
                column: "Id",
                principalTable: "ProjectPlannings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_PromptGenerations_Id",
                table: "Reviews",
                column: "Id",
                principalTable: "PromptGenerations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_RequirementsAnalyses_Id",
                table: "Reviews",
                column: "Id",
                principalTable: "RequirementsAnalyses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_StoryGenerations_Id",
                table: "Reviews",
                column: "Id",
                principalTable: "StoryGenerations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
