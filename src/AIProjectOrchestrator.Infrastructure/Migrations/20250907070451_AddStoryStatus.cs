using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIProjectOrchestrator.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStoryStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "UserStories",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "UserStories");
        }
    }
}
