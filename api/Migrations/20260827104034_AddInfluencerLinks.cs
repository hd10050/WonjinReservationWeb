using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WonjinApi.Migrations
{
    /// <inheritdoc />
    public partial class AddInfluencerLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "influencer_links",
                schema: "wonjin",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    utm_source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: ""),
                    utm_medium = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "influencer"),
                    utm_campaign = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: ""),
                    locale = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "zh-CN"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_influencer_links", x => x.id);
                    table.CheckConstraint("ck_influencer_links_locale", "locale IN ('zh-CN','zh-TW','en','ko')");
                });

            migrationBuilder.CreateIndex(
                name: "ix_influencer_links_is_active_created_at",
                schema: "wonjin",
                table: "influencer_links",
                columns: new[] { "is_active", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ux_influencer_links_code",
                schema: "wonjin",
                table: "influencer_links",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "influencer_links",
                schema: "wonjin");
        }
    }
}
