using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIProjectOrchestrator.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserStoryCascadeDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserStory_StoryGenerations_StoryGenerationId",
                table: "UserStory");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserStory",
                table: "UserStory");

            migrationBuilder.RenameTable(
                name: "UserStory",
                newName: "UserStories");

            migrationBuilder.RenameIndex(
                name: "IX_UserStory_StoryGenerationId",
                table: "UserStories",
                newName: "IX_UserStories_StoryGenerationId");

            migrationBuilder.AlterColumn<int>(
                name: "StoryGenerationId",
                table: "UserStories",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserStories",
                table: "UserStories",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserStories_StoryGenerations_StoryGenerationId",
                table: "UserStories",
                column: "StoryGenerationId",
                principalTable: "StoryGenerations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserStories_StoryGenerations_StoryGenerationId",
                table: "UserStories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserStories",
                table: "UserStories");

            migrationBuilder.RenameTable(
                name: "UserStories",
                newName: "UserStory");

            migrationBuilder.RenameIndex(
                name: "IX_UserStories_StoryGenerationId",
                table: "UserStory",
                newName: "IX_UserStory_StoryGenerationId");

            migrationBuilder.AlterColumn<int>(
                name: "StoryGenerationId",
                table: "UserStory",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserStory",
                table: "UserStory",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserStory_StoryGenerations_StoryGenerationId",
                table: "UserStory",
                column: "StoryGenerationId",
                principalTable: "StoryGenerations",
                principalColumn: "Id");
        }
    }
}
