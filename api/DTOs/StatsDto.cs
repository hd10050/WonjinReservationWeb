namespace WonjinApi.DTOs;

// Phase 6 — 실장 KPI(11-4절). 비활성 실장 제외(D13), 활성 실장은 기간 내 배정 0건이어도 0행으로 표시(11-6절 "구간 0 채움").
public record ConsultantKpiDto(
    int ConsultantId, string ConsultantName,
    int Assigned, int Confirmed, int Visited, decimal ConversionRate);

// Phase 6 — 예약 통계 주간 추이(D16). 데이터 없는 주도 0으로 채워 내려준다(11-4절 완료기준).
public record WeeklyReservationStatDto(
    DateOnly WeekStart, int Received, int Confirmed, int Visited, int Cancelled);

public record ProcedureStatDto(
    int ProcedureId, string NameZhCn, string NameZhTw, string NameEn, string NameKo, int Count);

public record LocaleStatDto(string Locale, int Count);

// 담당 실장 축(11-4절 "담당 실장 축으로 나눌 때는 비활성 실장 제외") — 시술별/언어별과 같은 표+막대 구성이라
// KPI(ConsultantKpiDto)처럼 0행 채움은 하지 않고 실적 있는 실장만 내려준다.
public record ConsultantReservationStatDto(int ConsultantId, string ConsultantName, int Count);

public record ReservationStatsDto(
    List<WeeklyReservationStatDto> Weekly,
    List<ProcedureStatDto> Procedures,
    List<LocaleStatDto> Locales,
    List<ConsultantReservationStatDto> Consultants);

// Phase 8 — 유입 경로 분석(D4·D5, 15-2절). 어드민 전용. landing_daily_stats 조합을 기준으로 그룹핑하고
// (15-2절 "추천코드/UTM 조합 | landing_daily_stats 그룹"), 같은 조합의 reservations 건수를 매핑한다.
// 실적 있는 조합만 내려준다 — ConsultantReservationStatDto와 동일하게 마스터 테이블이 없어 0행 채움 대상이 아니다.
public record ReferralStatDto(
    string ReferralCode, string UtmSource, string UtmMedium, string UtmCampaign,
    int VisitCount, int ReservationCount, decimal ConversionRate,
    int ConfirmedCount, decimal ConfirmedConversionRate);
