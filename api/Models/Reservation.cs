namespace WonjinApi.Models;

// 핵심 테이블. 소프트 삭제는 AppDbContext의 전역 쿼리 필터(deleted_at IS NULL)로 강제한다(D15).
public class Reservation
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty; // YYYYMMDD+4자리, 8-11절 카운터로 발급
    public string Name { get; set; } = string.Empty;
    public DateOnly BirthDate { get; set; }
    public string Gender { get; set; } = string.Empty; // Female | Male | Other
    public string WechatId { get; set; } = string.Empty;
    // 고객 직접 입력, KST 벽시계 날짜·시각 (D10). Date는 2026-08-28 추가 — 라이브 서비스 기존 행
    // 호환을 위해 NULL 허용이되 신규 제출은 프론트·백엔드 모두 필수(visit_date와 동일 취급).
    // 🔴 D26(2026-08-28) — "상관없음" 체크 시 둘 다 NULL로 저장(시각 무관 표시).
    public DateOnly? PreferredContactDate { get; set; }
    public TimeOnly? PreferredContactTime { get; set; }
    public string Locale { get; set; } = string.Empty;
    public string Status { get; set; } = "New"; // New | Consulting | Confirmed | Visited | Cancelled

    public int? ConsultantId { get; set; } // NULL이면 미배정 — D17: 배정 전엔 업무 입력 전부 차단
    public Consultant? Consultant { get; set; }

    public DateOnly? VisitDate { get; set; }
    public TimeOnly? VisitTime { get; set; }
    public decimal? DepositAmount { get; set; }
    public string DepositCurrency { get; set; } = "CNY"; // CNY | KRW, 환율 환산 안 함(D12)
    public bool DepositPaid { get; set; }
    public string? CancelReason { get; set; }

    public string UtmSource { get; set; } = string.Empty;
    public string UtmMedium { get; set; } = string.Empty;
    public string UtmCampaign { get; set; } = string.Empty;
    public string ReferralCode { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? ConsultingAt { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public DateTimeOffset? VisitedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; } // D15 소프트 삭제
    public int? DeletedByUserId { get; set; }
    public User? DeletedByUser { get; set; }

    public ICollection<ReservationProcedure> ReservationProcedures { get; set; } = [];
    public ICollection<ReservationNote> Notes { get; set; } = [];
    public ICollection<ReservationLog> Logs { get; set; } = [];

    // 낙관적 동시성 토큰 — PostgreSQL 시스템 컬럼 xmin에 매핑(AppDbContext에서 IsRowVersion() 설정).
    // 새 컬럼을 추가하는 게 아니라 이미 모든 행에 존재하는 값을 노출만 하는 것이라 저장공간 증가 없음.
    public uint RowVersion { get; set; }
}
