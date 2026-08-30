using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WonjinApi.Migrations
{
    /// <inheritdoc />
    public partial class AddQueryPerfIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_reservations_visited_at",
                schema: "wonjin",
                table: "reservations",
                column: "visited_at",
                filter: "status = 'Visited'");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_is_revoked_expires_at",
                schema: "wonjin",
                table: "refresh_tokens",
                columns: new[] { "is_revoked", "expires_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_reservations_visited_at",
                schema: "wonjin",
                table: "reservations");

            migrationBuilder.DropIndex(
                name: "ix_refresh_tokens_is_revoked_expires_at",
                schema: "wonjin",
                table: "refresh_tokens");
        }
    }
}
