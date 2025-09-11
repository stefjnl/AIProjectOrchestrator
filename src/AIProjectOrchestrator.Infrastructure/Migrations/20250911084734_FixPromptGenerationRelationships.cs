using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIProjectOrchestrator.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixPromptGenerationRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PromptGenerations_StoryGenerations_StoryGenerationId",
                table: "PromptGenerations");

            migrationBuilder.RenameIndex(
                name: "IX_PromptGeneration_StoryGenerationId",
                table: "PromptGenerations",
                newName: "IX_PromptGenerations_StoryGenerationId");

            migrationBuilder.AlterColumn<int>(
                name: "StoryGenerationId",
                table: "PromptGenerations",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<Guid>(
                name: "UserStoryId",
                table: "PromptGenerations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_PromptGeneration_UserStoryId",
                table: "PromptGenerations",
                column: "UserStoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_PromptGenerations_StoryGenerations_StoryGenerationId",
                table: "PromptGenerations",
                column: "StoryGenerationId",
                principalTable: "StoryGenerations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PromptGenerations_UserStories_UserStoryId",
                table: "PromptGenerations",
                column: "UserStoryId",
                principalTable: "UserStories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PromptGenerations_StoryGenerations_StoryGenerationId",
                table: "PromptGenerations");

            migrationBuilder.DropForeignKey(
                name: "FK_PromptGenerations_UserStories_UserStoryId",
                table: "PromptGenerations");

            migrationBuilder.DropIndex(
                name: "IX_PromptGeneration_UserStoryId",
                table: "PromptGenerations");

            migrationBuilder.DropColumn(
                name: "UserStoryId",
                table: "PromptGenerations");

            migrationBuilder.RenameIndex(
                name: "IX_PromptGenerations_StoryGenerationId",
                table: "PromptGenerations",
                newName: "IX_PromptGeneration_StoryGenerationId");

            migrationBuilder.AlterColumn<int>(
                name: "StoryGenerationId",
                table: "PromptGenerations",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PromptGenerations_StoryGenerations_StoryGenerationId",
                table: "PromptGenerations",
                column: "StoryGenerationId",
                principalTable: "StoryGenerations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
