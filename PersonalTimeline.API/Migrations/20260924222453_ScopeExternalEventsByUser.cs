using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalTimeline.API.Migrations
{
    /// <inheritdoc />
    public partial class ScopeExternalEventsByUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TimelineEntries_SourceApi_ExternalId",
                table: "TimelineEntries");

            migrationBuilder.DropIndex(
                name: "IX_TimelineEntries_UserId",
                table: "TimelineEntries");

            migrationBuilder.CreateIndex(
                name: "IX_TimelineEntries_UserId_SourceApi_ExternalId",
                table: "TimelineEntries",
                columns: new[] { "UserId", "SourceApi", "ExternalId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TimelineEntries_UserId_SourceApi_ExternalId",
                table: "TimelineEntries");

            migrationBuilder.CreateIndex(
                name: "IX_TimelineEntries_SourceApi_ExternalId",
                table: "TimelineEntries",
                columns: new[] { "SourceApi", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TimelineEntries_UserId",
                table: "TimelineEntries",
                column: "UserId");
        }
    }
}
