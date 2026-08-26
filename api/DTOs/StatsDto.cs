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
