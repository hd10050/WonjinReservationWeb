using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WonjinApi.Migrations
{
    /// <inheritdoc />
    public partial class AddPreferredContactDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "preferred_contact_date",
                schema: "wonjin",
                table: "reservations",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "preferred_contact_date",
                schema: "wonjin",
                table: "reservations");
        }
    }
}
