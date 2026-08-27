using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WonjinApi.Migrations
{
    /// <inheritdoc />
    public partial class AddCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_procedures_is_active_sort_order",
                schema: "wonjin",
                table: "procedures");

            migrationBuilder.DropColumn(
                name: "sort_order",
                schema: "wonjin",
                table: "procedures");

            migrationBuilder.AddColumn<int>(
                name: "category_id",
                schema: "wonjin",
                table: "procedures",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "categories",
                schema: "wonjin",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name_zh_cn = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name_zh_tw = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name_en = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name_ko = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categories", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_procedures_category_id",
                schema: "wonjin",
                table: "procedures",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_procedures_is_active",
                schema: "wonjin",
                table: "procedures",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_categories_is_active",
                schema: "wonjin",
                table: "categories",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ux_categories_code",
                schema: "wonjin",
                table: "categories",
                column: "code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_procedures_categories_category_id",
                schema: "wonjin",
                table: "procedures",
                column: "category_id",
                principalSchema: "wonjin",
                principalTable: "categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_procedures_categories_category_id",
                schema: "wonjin",
                table: "procedures");

            migrationBuilder.DropTable(
                name: "categories",
                schema: "wonjin");

            migrationBuilder.DropIndex(
                name: "ix_procedures_category_id",
                schema: "wonjin",
                table: "procedures");

            migrationBuilder.DropIndex(
                name: "ix_procedures_is_active",
                schema: "wonjin",
                table: "procedures");

            migrationBuilder.DropColumn(
                name: "category_id",
                schema: "wonjin",
                table: "procedures");

            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                schema: "wonjin",
                table: "procedures",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_procedures_is_active_sort_order",
                schema: "wonjin",
                table: "procedures",
                columns: new[] { "is_active", "sort_order" });
        }
    }
}
