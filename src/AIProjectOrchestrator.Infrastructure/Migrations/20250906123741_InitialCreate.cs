using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AIProjectOrchestrator.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RequirementsAnalyses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectId = table.Column<int>(type: "integer", nullable: false),
                    AnalysisId = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    ReviewId = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequirementsAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequirementsAnalyses_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectPlannings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequirementsAnalysisId = table.Column<int>(type: "integer", nullable: false),
                    PlanningId = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    ReviewId = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectPlannings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectPlannings_RequirementsAnalyses_RequirementsAnalysisId",
                        column: x => x.RequirementsAnalysisId,
                        principalTable: "RequirementsAnalyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StoryGenerations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectPlanningId = table.Column<int>(type: "integer", nullable: false),
                    GenerationId = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    ReviewId = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoryGenerations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoryGenerations_ProjectPlannings_ProjectPlanningId",
                        column: x => x.ProjectPlanningId,
                        principalTable: "ProjectPlannings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PromptGenerations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StoryGenerationId = table.Column<int>(type: "integer", nullable: false),
                    StoryIndex = table.Column<int>(type: "integer", nullable: false),
                    PromptId = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    ReviewId = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromptGenerations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromptGenerations_StoryGenerations_StoryGenerationId",
                        column: x => x.StoryGenerationId,
                        principalTable: "StoryGenerations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserStory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    AcceptanceCriteria = table.Column<List<string>>(type: "text[]", nullable: false),
                    Priority = table.Column<string>(type: "text", nullable: false),
                    StoryPoints = table.Column<int>(type: "integer", nullable: true),
                    Tags = table.Column<List<string>>(type: "text[]", nullable: false),
                    EstimatedComplexity = table.Column<string>(type: "text", nullable: true),
                    StoryGenerationId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserStory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserStory_StoryGenerations_StoryGenerationId",
                        column: x => x.StoryGenerationId,
                        principalTable: "StoryGenerations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    ReviewId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ServiceName = table.Column<string>(type: "text", nullable: false),
                    PipelineStage = table.Column<string>(type: "text", nullable: false),
                    Feedback = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reviews_ProjectPlannings_Id",
                        column: x => x.Id,
                        principalTable: "ProjectPlannings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reviews_PromptGenerations_Id",
                        column: x => x.Id,
                        principalTable: "PromptGenerations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reviews_RequirementsAnalyses_Id",
                        column: x => x.Id,
                        principalTable: "RequirementsAnalyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reviews_StoryGenerations_Id",
                        column: x => x.Id,
                        principalTable: "StoryGenerations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPlannings_PlanningId",
                table: "ProjectPlannings",
                column: "PlanningId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPlannings_RequirementsAnalysisId",
                table: "ProjectPlannings",
                column: "RequirementsAnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Name",
                table: "Projects",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_PromptGenerations_PromptId",
                table: "PromptGenerations",
                column: "PromptId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PromptGenerations_StoryGenerationId",
                table: "PromptGenerations",
                column: "StoryGenerationId");

            migrationBuilder.CreateIndex(
                name: "IX_RequirementsAnalyses_AnalysisId",
                table: "RequirementsAnalyses",
                column: "AnalysisId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequirementsAnalyses_ProjectId",
                table: "RequirementsAnalyses",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_PipelineStage",
                table: "Reviews",
                column: "PipelineStage");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ReviewId",
                table: "Reviews",
                column: "ReviewId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ServiceName",
                table: "Reviews",
                column: "ServiceName");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_Status",
                table: "Reviews",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_StoryGenerations_GenerationId",
                table: "StoryGenerations",
                column: "GenerationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoryGenerations_ProjectPlanningId",
                table: "StoryGenerations",
                column: "ProjectPlanningId");

            migrationBuilder.CreateIndex(
                name: "IX_UserStory_StoryGenerationId",
                table: "UserStory",
                column: "StoryGenerationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reviews");

            migrationBuilder.DropTable(
                name: "UserStory");

            migrationBuilder.DropTable(
                name: "PromptGenerations");

            migrationBuilder.DropTable(
                name: "StoryGenerations");

            migrationBuilder.DropTable(
                name: "ProjectPlannings");

            migrationBuilder.DropTable(
                name: "RequirementsAnalyses");

            migrationBuilder.DropTable(
                name: "Projects");
        }
    }
}
