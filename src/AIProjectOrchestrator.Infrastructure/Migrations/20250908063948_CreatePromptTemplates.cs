using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIProjectOrchestrator.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreatePromptTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PromptTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromptTemplates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PromptTemplates_Title",
                table: "PromptTemplates",
                column: "Title");

            migrationBuilder.Sql($@"
                INSERT INTO ""PromptTemplates"" (""Id"", ""Title"", ""Content"", ""CreatedAt"", ""UpdatedAt"")
                VALUES ('11111111-1111-1111-1111-111111111111', 'Email Draft', 'Write a professional email about {{topic}} to {{recipient}} including the following points: {{points}}. Make it concise and polite.', '{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}', NULL);");

            migrationBuilder.Sql($@"
                INSERT INTO ""PromptTemplates"" (""Id"", ""Title"", ""Content"", ""CreatedAt"", ""UpdatedAt"")
                VALUES ('22222222-2222-2222-2222-222222222222', 'Code Review', 'Review this code for best practices, security issues, and performance improvements. Provide specific line numbers and suggestions for improvement.', '{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}', NULL);");

            migrationBuilder.Sql($@"
                INSERT INTO ""PromptTemplates"" (""Id"", ""Title"", ""Content"", ""CreatedAt"", ""UpdatedAt"")
                VALUES ('33333333-3333-3333-3333-333333333333', 'Meeting Summary', 'Summarize the key points from this meeting transcript. Include action items, decisions made, and next steps. Structure the summary with bullet points.', '{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}', NULL);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PromptTemplates");
        }
    }
}
