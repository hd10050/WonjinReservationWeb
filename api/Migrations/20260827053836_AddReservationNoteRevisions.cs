using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WonjinApi.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationNoteRevisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reservation_note_revisions",
                schema: "wonjin",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    reservation_note_id = table.Column<int>(type: "integer", nullable: false),
                    body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    edited_by_user_id = table.Column<int>(type: "integer", nullable: true),
                    edited_by_name = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    edited_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reservation_note_revisions", x => x.id);
                    table.ForeignKey(
                        name: "fk_reservation_note_revisions_reservation_notes_reservation_no",
                        column: x => x.reservation_note_id,
                        principalSchema: "wonjin",
                        principalTable: "reservation_notes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_reservation_note_revisions_users_edited_by_user_id",
                        column: x => x.edited_by_user_id,
                        principalSchema: "wonjin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_reservation_note_revisions_edited_by_user_id",
                schema: "wonjin",
                table: "reservation_note_revisions",
                column: "edited_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_reservation_note_revisions_note_id_edited_at",
                schema: "wonjin",
                table: "reservation_note_revisions",
                columns: new[] { "reservation_note_id", "edited_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reservation_note_revisions",
                schema: "wonjin");
        }
    }
}
