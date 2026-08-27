using Microsoft.EntityFrameworkCore;
using WonjinApi.Models;

namespace WonjinApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Procedure> Procedures => Set<Procedure>();
    public DbSet<Consultant> Consultants => Set<Consultant>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<ReservationProcedure> ReservationProcedures => Set<ReservationProcedure>();
    public DbSet<ReservationNote> ReservationNotes => Set<ReservationNote>();
    public DbSet<ReservationLog> ReservationLogs => Set<ReservationLog>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<LandingDailyStat> LandingDailyStats => Set<LandingDailyStat>();
    public DbSet<ReservationCodeCounter> ReservationCodeCounters => Set<ReservationCodeCounter>();
    public DbSet<WebPushSubscription> WebPushSubscriptions => Set<WebPushSubscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("wonjin");

        // ── users (8-1) ──────────────────────────────────────────
        modelBuilder.Entity<User>(e =>
        {
            e.Property(u => u.Email).HasMaxLength(254).IsRequired();
            e.Property(u => u.PasswordHash).HasMaxLength(100).IsRequired();
            e.Property(u => u.Role).HasMaxLength(20).IsRequired();
            e.Property(u => u.Name).HasMaxLength(30).IsRequired();
            e.Property(u => u.Locale).HasMaxLength(10).IsRequired().HasDefaultValue("ko");
            e.Property(u => u.IsSuspended).HasDefaultValue(false);

            e.HasIndex(u => u.Email).IsUnique().HasDatabaseName("ux_users_email");
            e.HasIndex(u => u.Role).HasDatabaseName("ix_users_role");

            e.ToTable(t =>
            {
                t.HasCheckConstraint("ck_users_role", "role IN ('Admin','HospitalManager','Consultant')");
                t.HasCheckConstraint("ck_users_locale", "locale IN ('zh-CN','zh-TW','en','ko')");
            });
        });

        // ── refresh_tokens (8-2) ─────────────────────────────────
        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.Property(r => r.TokenHash).HasMaxLength(64).IsRequired();
            e.Property(r => r.IsRevoked).HasDefaultValue(false);

            e.HasOne(r => r.User).WithMany(u => u.RefreshTokens)
                .HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);

            // 🔴 모든 세션이 12분마다 조회 — 없으면 갱신마다 풀스캔(8-2절)
            e.HasIndex(r => r.TokenHash).HasDatabaseName("ix_refresh_tokens_token_hash");
            e.HasIndex(r => r.UserId).HasDatabaseName("ix_refresh_tokens_user_id");
        });

        // ── procedures (8-3) ─────────────────────────────────────
        modelBuilder.Entity<Procedure>(e =>
        {
            e.Property(p => p.Code).HasMaxLength(30).IsRequired();
            e.Property(p => p.NameZhCn).HasMaxLength(50).IsRequired();
            e.Property(p => p.NameZhTw).HasMaxLength(50).IsRequired();
            e.Property(p => p.NameEn).HasMaxLength(50).IsRequired();
            e.Property(p => p.NameKo).HasMaxLength(50).IsRequired();
            e.Property(p => p.SortOrder).HasDefaultValue(0);
            e.Property(p => p.IsActive).HasDefaultValue(true);

            e.HasIndex(p => p.Code).IsUnique().HasDatabaseName("ux_procedures_code");
            e.HasIndex(p => new { p.IsActive, p.SortOrder }).HasDatabaseName("ix_procedures_is_active_sort_order");
        });

        // ── consultants (8-4) ────────────────────────────────────
        modelBuilder.Entity<Consultant>(e =>
        {
            e.Property(c => c.Name).HasMaxLength(30).IsRequired();
            e.Property(c => c.SortOrder).HasDefaultValue(0);
            e.Property(c => c.IsActive).HasDefaultValue(true);

            e.HasIndex(c => new { c.IsActive, c.SortOrder }).HasDatabaseName("ix_consultants_is_active_sort_order");
        });

        // ── reservations (8-5) ───────────────────────────────────
        modelBuilder.Entity<Reservation>(e =>
        {
            e.Property(r => r.Code).HasMaxLength(12).IsRequired();
            e.Property(r => r.Name).HasMaxLength(50).IsRequired();
            e.Property(r => r.Gender).HasMaxLength(10).IsRequired();
            e.Property(r => r.WechatId).HasMaxLength(50).IsRequired();
            e.Property(r => r.Locale).HasMaxLength(10).IsRequired();
            e.Property(r => r.Status).HasMaxLength(20).IsRequired().HasDefaultValue("New");
            e.Property(r => r.DepositAmount).HasPrecision(12, 2);
            e.Property(r => r.DepositCurrency).HasMaxLength(3).IsRequired().HasDefaultValue("CNY");
            e.Property(r => r.DepositPaid).HasDefaultValue(false);
            e.Property(r => r.CancelReason).HasMaxLength(200);
            e.Property(r => r.UtmSource).HasMaxLength(100).IsRequired().HasDefaultValue("");
            e.Property(r => r.UtmMedium).HasMaxLength(100).IsRequired().HasDefaultValue("");
            e.Property(r => r.UtmCampaign).HasMaxLength(100).IsRequired().HasDefaultValue("");
            e.Property(r => r.ReferralCode).HasMaxLength(50).IsRequired().HasDefaultValue("");
            // AssignConsultant 동시 재배정 시 로그의 "이전 담당자명" 정확성 보장(보안감사 2026-08-26 TODO,
            // 2026-08-27 도입) — PostgreSQL 시스템 컬럼 xmin을 낙관적 동시성 토큰으로 사용. 새 컬럼을
            // 만드는 게 아니라 이미 존재하는 시스템 컬럼을 노출하는 것뿐이라 마이그레이션이 AddColumn을
            // 생성하지 않아야 정상(Npgsql.EntityFrameworkCore.PostgreSQL 공식 문서 패턴).
            e.Property(r => r.RowVersion).IsRowVersion();

            e.HasOne(r => r.Consultant).WithMany(c => c.Reservations)
                .HasForeignKey(r => r.ConsultantId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.DeletedByUser).WithMany()
                .HasForeignKey(r => r.DeletedByUserId).OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(r => r.Code).IsUnique().HasDatabaseName("ux_reservations_code");
            e.HasIndex(r => new { r.Status, r.CreatedAt }).HasDatabaseName("ix_reservations_status_created_at");
            e.HasIndex(r => r.CreatedAt).HasDatabaseName("ix_reservations_created_at");
            e.HasIndex(r => new { r.ConsultantId, r.Status }).HasDatabaseName("ix_reservations_consultant_id_status");
            // 부분 인덱스 — [예약 달력] 월간 조회(D17 확정: Confirmed+Visited 둘 다 표시, F1)
            e.HasIndex(r => r.VisitDate)
                .HasDatabaseName("ix_reservations_visit_date")
                .HasFilter("status IN ('Confirmed','Visited')");

            // 🔴 소프트 삭제 전역 쿼리 필터(D15) — 조회마다 손으로 deleted_at 조건을 붙이지 않게 함
            e.HasQueryFilter(r => r.DeletedAt == null);

            e.ToTable(t =>
            {
                t.HasCheckConstraint("ck_reservations_gender", "gender IN ('Female','Male','Other')");
                t.HasCheckConstraint("ck_reservations_status", "status IN ('New','Consulting','Confirmed','Visited','Cancelled')");
                t.HasCheckConstraint("ck_reservations_deposit_currency", "deposit_currency IN ('CNY','KRW')");
                t.HasCheckConstraint("ck_reservations_deposit_amount", "deposit_amount >= 0");
            });
        });

        // ── reservation_procedures (8-6, M:N 복합 PK) ────────────
        modelBuilder.Entity<ReservationProcedure>(e =>
        {
            e.HasKey(rp => new { rp.ReservationId, rp.ProcedureId });

            e.HasOne(rp => rp.Reservation).WithMany(r => r.ReservationProcedures)
                .HasForeignKey(rp => rp.ReservationId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(rp => rp.Procedure).WithMany(p => p.ReservationProcedures)
                .HasForeignKey(rp => rp.ProcedureId).OnDelete(DeleteBehavior.Restrict);

            // 복합 PK의 선행 컬럼이 reservation_id라 역방향(시술별 집계) 조회는 커버 안 됨 → 별도 인덱스
            e.HasIndex(rp => rp.ProcedureId).HasDatabaseName("ix_reservation_procedures_procedure_id");
        });

        // ── reservation_notes (8-7) ──────────────────────────────
        modelBuilder.Entity<ReservationNote>(e =>
        {
            e.Property(n => n.Body).HasMaxLength(2000).IsRequired();
            e.Property(n => n.AuthorName).HasMaxLength(30).IsRequired();

            e.HasOne(n => n.Reservation).WithMany(r => r.Notes)
                .HasForeignKey(n => n.ReservationId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(n => n.AuthorUser).WithMany()
                .HasForeignKey(n => n.AuthorUserId).OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(n => new { n.ReservationId, n.CreatedAt }).HasDatabaseName("ix_reservation_notes_reservation_id_created_at");
        });

        // ── reservation_logs (8-8) ───────────────────────────────
        modelBuilder.Entity<ReservationLog>(e =>
        {
            e.Property(l => l.Action).HasMaxLength(40).IsRequired();
            e.Property(l => l.Note).HasMaxLength(300);
            e.Property(l => l.ActorName).HasMaxLength(30).IsRequired();

            e.HasOne(l => l.Reservation).WithMany(r => r.Logs)
                .HasForeignKey(l => l.ReservationId).OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(l => new { l.ReservationId, l.CreatedAt }).HasDatabaseName("ix_reservation_logs_reservation_id_created_at");
        });

        // ── audit_logs (8-9) ─────────────────────────────────────
        modelBuilder.Entity<AuditLog>(e =>
        {
            e.Property(a => a.ActorEmail).HasMaxLength(254).IsRequired();
            e.Property(a => a.ActorRole).HasMaxLength(20).IsRequired();
            e.Property(a => a.Action).HasMaxLength(40).IsRequired();
            e.Property(a => a.EntityType).HasMaxLength(40).IsRequired();
            e.Property(a => a.EntityId).HasMaxLength(40);
            e.Property(a => a.Summary).HasMaxLength(300).IsRequired();
            e.Property(a => a.Ip).HasMaxLength(45);

            e.HasIndex(a => a.CreatedAt).HasDatabaseName("ix_audit_logs_created_at");
            e.HasIndex(a => new { a.ActorUserId, a.CreatedAt }).HasDatabaseName("ix_audit_logs_actor_user_id_created_at");
            e.HasIndex(a => new { a.EntityType, a.CreatedAt }).HasDatabaseName("ix_audit_logs_entity_type_created_at");
        });

        // ── landing_daily_stats (8-10) ───────────────────────────
        modelBuilder.Entity<LandingDailyStat>(e =>
        {
            e.Property(s => s.ReferralCode).HasMaxLength(50).IsRequired().HasDefaultValue("");
            e.Property(s => s.UtmSource).HasMaxLength(100).IsRequired().HasDefaultValue("");
            e.Property(s => s.UtmMedium).HasMaxLength(100).IsRequired().HasDefaultValue("");
            e.Property(s => s.UtmCampaign).HasMaxLength(100).IsRequired().HasDefaultValue("");
            e.Property(s => s.VisitCount).HasDefaultValue(0);

            // 🔴 키 컬럼 전부 NOT NULL DEFAULT ''로 둔 이유: PG UNIQUE는 NULL을 서로 다르게 취급(NULLS DISTINCT)
            e.HasIndex(s => new { s.StatDate, s.ReferralCode, s.UtmSource, s.UtmMedium, s.UtmCampaign })
                .IsUnique().HasDatabaseName("ux_landing_daily_stats_key");
            e.HasIndex(s => s.StatDate).HasDatabaseName("ix_landing_daily_stats_stat_date");
        });

        // ── reservation_code_counters (8-11) ─────────────────────
        modelBuilder.Entity<ReservationCodeCounter>(e =>
        {
            e.HasKey(c => c.CodeDate);
        });

        // ── web_push_subscriptions — 새 예약 접수 알림 전용(어드민 내부, 공개 마케팅 아님) ──
        modelBuilder.Entity<WebPushSubscription>(e =>
        {
            e.Property(s => s.Endpoint).HasMaxLength(500).IsRequired();
            e.Property(s => s.P256dh).HasMaxLength(200).IsRequired();
            e.Property(s => s.Auth).HasMaxLength(200).IsRequired();

            e.HasOne(s => s.User).WithMany()
                .HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(s => s.Endpoint).IsUnique().HasDatabaseName("ux_web_push_subscriptions_endpoint");
            // 발송 시 UserId로 JOIN해 활성 계정만 거르므로(4-9절) 인덱스 필요
            e.HasIndex(s => s.UserId).HasDatabaseName("ix_web_push_subscriptions_user_id");
        });
    }
}
