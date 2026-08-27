using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WonjinApi.Migrations
{
    /// <inheritdoc />
    public partial class MakePreferredContactTimeNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<TimeOnly>(
                name: "preferred_contact_time",
                schema: "wonjin",
                table: "reservations",
                type: "time without time zone",
                nullable: true,
                oldClrType: typeof(TimeOnly),
                oldType: "time without time zone");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<TimeOnly>(
                name: "preferred_contact_time",
                schema: "wonjin",
                table: "reservations",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0),
                oldClrType: typeof(TimeOnly),
                oldType: "time without time zone",
                oldNullable: true);
        }
    }
}
