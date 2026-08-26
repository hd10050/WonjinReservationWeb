using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WonjinApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "wonjin");

            migrationBuilder.CreateTable(
                name: "audit_logs",
                schema: "wonjin",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    actor_user_id = table.Column<int>(type: "integer", nullable: true),
                    actor_email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    actor_role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    action = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    entity_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    summary = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    status_code = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "consultants",
                schema: "wonjin",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_consultants", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "landing_daily_stats",
                schema: "wonjin",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    stat_date = table.Column<DateOnly>(type: "date", nullable: false),
                    referral_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: ""),
                    utm_source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: ""),
                    utm_medium = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: ""),
                    utm_campaign = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: ""),
                    visit_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_landing_daily_stats", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "procedures",
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
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_procedures", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "reservation_code_counters",
                schema: "wonjin",
                columns: table => new
                {
                    code_date = table.Column<DateOnly>(type: "date", nullable: false),
                    last_seq = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reservation_code_counters", x => x.code_date);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "wonjin",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    locale = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "ko"),
                    is_suspended = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.CheckConstraint("ck_users_locale", "locale IN ('zh-CN','zh-TW','en','ko')");
                    table.CheckConstraint("ck_users_role", "role IN ('Admin','HospitalManager','Consultant')");
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                schema: "wonjin",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_revoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "wonjin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reservations",
                schema: "wonjin",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    birth_date = table.Column<DateOnly>(type: "date", nullable: false),
                    gender = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    wechat_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    preferred_contact_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    locale = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "New"),
                    consultant_id = table.Column<int>(type: "integer", nullable: true),
                    visit_date = table.Column<DateOnly>(type: "date", nullable: true),
                    visit_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    deposit_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    deposit_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "CNY"),
                    deposit_paid = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    cancel_reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    utm_source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: ""),
                    utm_medium = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: ""),
                    utm_campaign = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: ""),
                    referral_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: ""),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    consulting_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    confirmed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    visited_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reservations", x => x.id);
                    table.CheckConstraint("ck_reservations_deposit_amount", "deposit_amount >= 0");
                    table.CheckConstraint("ck_reservations_deposit_currency", "deposit_currency IN ('CNY','KRW')");
                    table.CheckConstraint("ck_reservations_gender", "gender IN ('Female','Male','Other')");
                    table.CheckConstraint("ck_reservations_status", "status IN ('New','Consulting','Confirmed','Visited','Cancelled')");
                    table.ForeignKey(
                        name: "fk_reservations_consultants_consultant_id",
                        column: x => x.consultant_id,
                        principalSchema: "wonjin",
                        principalTable: "consultants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_reservations_users_deleted_by_user_id",
                        column: x => x.deleted_by_user_id,
                        principalSchema: "wonjin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "reservation_logs",
                schema: "wonjin",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    reservation_id = table.Column<int>(type: "integer", nullable: false),
                    action = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    note = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    actor_user_id = table.Column<int>(type: "integer", nullable: true),
                    actor_name = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reservation_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_reservation_logs_reservations_reservation_id",
                        column: x => x.reservation_id,
                        principalSchema: "wonjin",
                        principalTable: "reservations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reservation_notes",
                schema: "wonjin",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    reservation_id = table.Column<int>(type: "integer", nullable: false),
                    body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    author_user_id = table.Column<int>(type: "integer", nullable: true),
                    author_name = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reservation_notes", x => x.id);
                    table.ForeignKey(
                        name: "fk_reservation_notes_reservations_reservation_id",
                        column: x => x.reservation_id,
                        principalSchema: "wonjin",
                        principalTable: "reservations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_reservation_notes_users_author_user_id",
                        column: x => x.author_user_id,
                        principalSchema: "wonjin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "reservation_procedures",
                schema: "wonjin",
                columns: table => new
                {
                    reservation_id = table.Column<int>(type: "integer", nullable: false),
                    procedure_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reservation_procedures", x => new { x.reservation_id, x.procedure_id });
                    table.ForeignKey(
                        name: "fk_reservation_procedures_procedures_procedure_id",
                        column: x => x.procedure_id,
                        principalSchema: "wonjin",
                        principalTable: "procedures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_reservation_procedures_reservations_reservation_id",
                        column: x => x.reservation_id,
                        principalSchema: "wonjin",
                        principalTable: "reservations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_actor_user_id_created_at",
                schema: "wonjin",
                table: "audit_logs",
                columns: new[] { "actor_user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_created_at",
                schema: "wonjin",
                table: "audit_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_entity_type_created_at",
                schema: "wonjin",
                table: "audit_logs",
                columns: new[] { "entity_type", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_consultants_is_active_sort_order",
                schema: "wonjin",
                table: "consultants",
                columns: new[] { "is_active", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_landing_daily_stats_stat_date",
                schema: "wonjin",
                table: "landing_daily_stats",
                column: "stat_date");

            migrationBuilder.CreateIndex(
                name: "ux_landing_daily_stats_key",
                schema: "wonjin",
                table: "landing_daily_stats",
                columns: new[] { "stat_date", "referral_code", "utm_source", "utm_medium", "utm_campaign" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_procedures_is_active_sort_order",
                schema: "wonjin",
                table: "procedures",
                columns: new[] { "is_active", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ux_procedures_code",
                schema: "wonjin",
                table: "procedures",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_token_hash",
                schema: "wonjin",
                table: "refresh_tokens",
                column: "token_hash");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_user_id",
                schema: "wonjin",
                table: "refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_reservation_logs_reservation_id_created_at",
                schema: "wonjin",
                table: "reservation_logs",
                columns: new[] { "reservation_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_reservation_notes_author_user_id",
                schema: "wonjin",
                table: "reservation_notes",
                column: "author_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_reservation_notes_reservation_id_created_at",
                schema: "wonjin",
                table: "reservation_notes",
                columns: new[] { "reservation_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_reservation_procedures_procedure_id",
                schema: "wonjin",
                table: "reservation_procedures",
                column: "procedure_id");

            migrationBuilder.CreateIndex(
                name: "ix_reservations_consultant_id_status",
                schema: "wonjin",
                table: "reservations",
                columns: new[] { "consultant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_reservations_created_at",
                schema: "wonjin",
                table: "reservations",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_reservations_deleted_by_user_id",
                schema: "wonjin",
                table: "reservations",
                column: "deleted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_reservations_status_created_at",
                schema: "wonjin",
                table: "reservations",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_reservations_visit_date",
                schema: "wonjin",
                table: "reservations",
                column: "visit_date",
                filter: "status IN ('Confirmed','Visited')");

            migrationBuilder.CreateIndex(
                name: "ux_reservations_code",
                schema: "wonjin",
                table: "reservations",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_role",
                schema: "wonjin",
                table: "users",
                column: "role");

            migrationBuilder.CreateIndex(
                name: "ux_users_email",
                schema: "wonjin",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs",
                schema: "wonjin");

            migrationBuilder.DropTable(
                name: "landing_daily_stats",
                schema: "wonjin");

            migrationBuilder.DropTable(
                name: "refresh_tokens",
                schema: "wonjin");

            migrationBuilder.DropTable(
                name: "reservation_code_counters",
                schema: "wonjin");

            migrationBuilder.DropTable(
                name: "reservation_logs",
                schema: "wonjin");

            migrationBuilder.DropTable(
                name: "reservation_notes",
                schema: "wonjin");

            migrationBuilder.DropTable(
                name: "reservation_procedures",
                schema: "wonjin");

            migrationBuilder.DropTable(
                name: "procedures",
                schema: "wonjin");

            migrationBuilder.DropTable(
                name: "reservations",
                schema: "wonjin");

            migrationBuilder.DropTable(
                name: "consultants",
                schema: "wonjin");

            migrationBuilder.DropTable(
                name: "users",
                schema: "wonjin");
        }
    }
}
