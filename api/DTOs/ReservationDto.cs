using System.ComponentModel.DataAnnotations;

namespace WonjinApi.DTOs;

// 공개 예약 신청(11-1절). record 검증 애노테이션은 파라미터에 직접 부착할 것 — [property: ...]는
// 런타임 500을 던진다(11-8절 함정, 실측 확인).
public record ReservationCreateRequest(
    [Required, MaxLength(50)] string Name,
    DateOnly BirthDate,
    [Required] string Gender,
    [Required, MaxLength(50)] string WechatId,
    TimeOnly PreferredContactTime,
    [Required] string Locale,
    bool PrivacyConsent,
    string? Honeypot,
    string? UtmSource,
    string? UtmMedium,
    string? UtmCampaign,
    string? ReferralCode
);

public record ReservationCreateResponse(string Code, string WechatId);

public record ReservationListItemDto(
    int Id, string Code, string Name, string WechatId, string Status,
    int? ConsultantId, string? ConsultantName,
    DateTimeOffset CreatedAt, DateOnly? VisitDate);

public record ReservationSummaryDto(int New, int Consulting, int Confirmed, int VisitedThisMonth);

// [예약 달력] year·month 1개월치만 반환(12-6절) — VisitDate는 항상 있음(쿼리에서 이미 필터링됨)
public record ReservationCalendarItemDto(
    int Id, string Code, string Name, string Status,
    DateOnly VisitDate, TimeOnly? VisitTime, string? ConsultantName);

// 🔴 성능(2026-08-27) — 달력 그리드는 42일 전체 예약을 미리 다 받아왔었다(날짜 클릭 전에도).
// 그리드 배지("이 날짜에 N건")는 건수만 있으면 되므로, 상세 목록과 분리해 건수만 반환한다.
public record ReservationCalendarDayCountDto(DateOnly VisitDate, int Count);

public record ReservationNoteDto(
    int Id, string Body, int? AuthorUserId, string AuthorName,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, bool IsEdited);

public record ReservationNoteRevisionDto(int Id, string Body, string EditedByName, DateTimeOffset EditedAt);

public record ReservationLogDto(int Id, string Action, string? Note, string ActorName, DateTimeOffset CreatedAt);

public record ReservationDetailDto(
    int Id, string Code, string Name, DateOnly BirthDate, string Gender, string WechatId,
    TimeOnly PreferredContactTime, string Locale, string Status,
    int? ConsultantId, string? ConsultantName,
    DateOnly? VisitDate, TimeOnly? VisitTime,
    decimal? DepositAmount, string DepositCurrency, bool DepositPaid, string? CancelReason,
    string UtmSource, string UtmMedium, string UtmCampaign, string ReferralCode,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt,
    DateTimeOffset? ConsultingAt, DateTimeOffset? ConfirmedAt, DateTimeOffset? VisitedAt, DateTimeOffset? CancelledAt,
    int[] ProcedureIds,
    List<ReservationNoteDto> Notes,
    List<ReservationLogDto> Logs);

// 9-1절 3곳 일치: DB varchar(200)/varchar(3) — 아래 백엔드 검증과 프론트 maxlength가 이 값을 그대로 따른다.
// 🔴 DepositAmount 상한은 DB numeric(12,2)(AppDbContext.cs HasPrecision(12,2))와 반드시 일치시킬 것 —
// double.MaxValue로 뒀다가 큰 값 입력 시 400 대신 numeric overflow 500이 났다(재감사 발견).
// 🔴 범위 검증은 [Range] 대신 컨트롤러에서 수동으로 한다 — [ApiController]의 자동 ModelState 400은
// 앱 공용 {code} 응답 형식이 아닌 기본 ProblemDetails를 반환해, 프론트가 errCode()로 이를 못 읽고
// UNKNOWN(알 수 없는 오류)으로 표시했다(실사용 버그 리포트로 발견). 상·하한 값은 여기와
// AdminReservationsController.UpdateReservation 양쪽에 동일하게 유지할 것.
public record UpdateReservationRequest(
    DateOnly? VisitDate,
    TimeOnly? VisitTime,
    int[] ProcedureIds,
    decimal? DepositAmount,
    [Required, MaxLength(3)] string DepositCurrency,
    bool DepositPaid);

public record AssignConsultantRequest([Required] int ConsultantId);

public record ChangeStatusRequest(
    [Required] string Status,
    [MaxLength(200)] string? CancelReason);

public record AddNoteRequest([Required, MaxLength(2000)] string Body);

public record UpdateNoteRequest([Required, MaxLength(2000)] string Body);

// 배정 드롭다운(예약 상세)·시술 선택 체크박스·Phase 4 관리 화면 목록이 공유하는 룩업 DTO.
public record ConsultantLookupDto(int Id, string Name, bool IsActive, int SortOrder);

// 🔴 D25(2026-08-28) — SortOrder 폐지, CategoryId 추가(예약 상세 아코디언이 이 값으로 카테고리별 그룹핑).
public record ProcedureLookupDto(
    int Id, string Code, int CategoryId, string NameZhCn, string NameZhTw, string NameEn, string NameKo, bool IsActive);
