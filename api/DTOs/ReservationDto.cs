using System.ComponentModel.DataAnnotations;

namespace WonjinApi.DTOs;

public record ReservationListItemDto(
    int Id, string Code, string Name, string WechatId, string Status,
    int? ConsultantId, string? ConsultantName,
    DateTimeOffset CreatedAt, DateOnly? VisitDate);

public record ReservationSummaryDto(int New, int Consulting, int Confirmed, int VisitedThisMonth);

public record ReservationNoteDto(
    int Id, string Body, int? AuthorUserId, string AuthorName,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, bool IsEdited);

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
public record UpdateReservationRequest(
    DateOnly? VisitDate,
    TimeOnly? VisitTime,
    int[] ProcedureIds,
    [Range(0, 9999999999.99)] decimal? DepositAmount,
    [Required, MaxLength(3)] string DepositCurrency,
    bool DepositPaid);

public record AssignConsultantRequest([Required] int ConsultantId);

public record ChangeStatusRequest(
    [Required] string Status,
    [MaxLength(200)] string? CancelReason);

public record AddNoteRequest([Required, MaxLength(2000)] string Body);

public record UpdateNoteRequest([Required, MaxLength(2000)] string Body);

// Phase 4 CRUD 이전 최소 조회 전용 — 배정/시술선택 드롭다운을 채우기 위한 룩업.
public record ConsultantLookupDto(int Id, string Name, bool IsActive, int SortOrder);

public record ProcedureLookupDto(
    int Id, string NameZhCn, string NameZhTw, string NameEn, string NameKo, bool IsActive, int SortOrder);
