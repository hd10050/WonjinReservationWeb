using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WonjinApi.Data;
using WonjinApi.DTOs;

namespace WonjinApi.Controllers;

// 통계(HospitalManager 이상, 11-4절·6-2절). 컨트롤러 전체가 조회(GET)뿐이라 6-3절 원칙1(쓰기 액션 액션레벨 재점검)은 해당 없음.
[ApiController]
[Route("api/admin/stats")]
[Authorize(Roles = "Admin,HospitalManager")]
public class AdminStatsController(AppDbContext db) : ControllerBase
{
    private static readonly TimeZoneInfo Kst = TimeZoneInfo.FindSystemTimeZoneById("Asia/Seoul");

    // 실장 KPI — 배정/확정/방문/확정전환율. 비활성 실장 제외(D13), 활성 실장은 기간 내 실적 0건이어도 0행 표시.
    [HttpGet("consultants")]
    public async Task<ActionResult<List<ConsultantKpiDto>>> GetConsultantKpi([FromQuery] DateOnly from, [FromQuery] DateOnly to)
    {
        // 🔴 [ApiController]는 DateOnly 미지정 시 400이 아니라 default(0001-01-01)로 조용히 바인딩한다(실측 확인
        // — 문서만으로 400을 단정했다가 실제로는 200이 나옴을 발견). 명시적으로 막지 않으면 파라미터를 빠뜨린
        // 요청이 "데이터 없음"처럼 보이는 잘못된 응답을 200으로 받는다.
        if (from == default || to == default || to < from)
            return BadRequest(new { code = "INVALID_DATE_RANGE" });

        var (fromUtc, toUtc) = ToKstRangeUtc(from, to);

        var activeConsultants = await GetActiveConsultantsAsync();
        var activeIds = activeConsultants.Select(c => c.Id).ToHashSet();

        // 1단계 — 익명 타입으로 집계(11-6절 함정: GroupBy().Select()에서 record 생성자 직접 호출 금지)
        var raw = await db.Reservations
            .Where(r => r.CreatedAt >= fromUtc && r.CreatedAt < toUtc
                     && r.ConsultantId != null && activeIds.Contains(r.ConsultantId.Value))
            .GroupBy(r => r.ConsultantId!.Value)
            .Select(g => new
            {
                ConsultantId = g.Key,
                Assigned = g.Count(),
                Confirmed = g.Count(r => r.Status == "Confirmed" || r.Status == "Visited"),
                Visited = g.Count(r => r.Status == "Visited"),
            })
            .ToListAsync();
        var byId = raw.ToDictionary(x => x.ConsultantId);

        // 2단계 — 메모리에서 DTO 매핑 + 활성 실장 전원 0행 채움(실적 0건인 실장도 목록에서 빠지면 안 됨, 11-6절)
        var items = activeConsultants.Select(c =>
        {
            byId.TryGetValue(c.Id, out var r);
            var assigned = r?.Assigned ?? 0;
            var confirmed = r?.Confirmed ?? 0;
            var visited = r?.Visited ?? 0;
            var rate = assigned == 0 ? 0m : Math.Round((decimal)confirmed / assigned * 100, 1);
            return new ConsultantKpiDto(c.Id, c.Name, assigned, confirmed, visited, rate);
        }).ToList();

        return Ok(items);
    }

    // 예약 통계 — 주간 추이(D16) + 시술별 집계 + 언어별 분포.
    [HttpGet("reservations")]
    public async Task<ActionResult<ReservationStatsDto>> GetReservationStats([FromQuery] DateOnly from, [FromQuery] DateOnly to)
    {
        // 🔴 [ApiController]는 DateOnly 미지정 시 400이 아니라 default(0001-01-01)로 조용히 바인딩한다(실측 확인
        // — GetConsultantKpi와 동일 사유).
        if (from == default || to == default || to < from)
            return BadRequest(new { code = "INVALID_DATE_RANGE" });

        var (fromUtc, toUtc) = ToKstRangeUtc(from, to);

        var weekly = await GetWeeklyStatsAsync(fromUtc, toUtc, from, to);

        // 시술별 집계 — db.Reservations(전역 소프트삭제 필터 적용됨)에서 SelectMany로 접근한다.
        // db.ReservationProcedures에서 바로 시작하면 8-5절 경고(필터 없는 자식에서 필터 걸린 부모 역참조) 위반이 된다.
        var procRaw = await db.Reservations
            .Where(r => r.CreatedAt >= fromUtc && r.CreatedAt < toUtc)
            .SelectMany(r => r.ReservationProcedures.Select(rp => rp.ProcedureId))
            .GroupBy(pid => pid)
            .Select(g => new { ProcedureId = g.Key, Count = g.Count() })
            .ToListAsync();
        var procIds = procRaw.Select(x => x.ProcedureId).ToList();
        var procNames = await db.Procedures.AsNoTracking()
            .Where(p => procIds.Contains(p.Id))
            .Select(p => new { p.Id, p.NameZhCn, p.NameZhTw, p.NameEn, p.NameKo })
            .ToListAsync();
        var procNameById = procNames.ToDictionary(p => p.Id);
        var procedures = procRaw
            .Select(x =>
            {
                procNameById.TryGetValue(x.ProcedureId, out var n);
                return new ProcedureStatDto(x.ProcedureId, n?.NameZhCn ?? "", n?.NameZhTw ?? "", n?.NameEn ?? "", n?.NameKo ?? "", x.Count);
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        // 언어별 분포 — locale은 CHECK 제약이 없는 자유 문자열(8-5절)이라 실제로 존재하는 값만 그룹핑한다.
        var localeRaw = await db.Reservations
            .Where(r => r.CreatedAt >= fromUtc && r.CreatedAt < toUtc)
            .GroupBy(r => r.Locale)
            .Select(g => new { Locale = g.Key, Count = g.Count() })
            .ToListAsync();
        var locales = localeRaw
            .Select(x => new LocaleStatDto(x.Locale, x.Count))
            .OrderByDescending(x => x.Count)
            .ToList();

        // 담당 실장 축(11-4절) — 비활성 실장 제외. KPI와 달리 0행 채움 없음(시술별·언어별과 동일하게 실적 있는 것만).
        var activeConsultants = await GetActiveConsultantsAsync();
        var activeIds = activeConsultants.Select(c => c.Id).ToHashSet();
        var consultantRaw = await db.Reservations
            .Where(r => r.CreatedAt >= fromUtc && r.CreatedAt < toUtc
                     && r.ConsultantId != null && activeIds.Contains(r.ConsultantId.Value))
            .GroupBy(r => r.ConsultantId!.Value)
            .Select(g => new { ConsultantId = g.Key, Count = g.Count() })
            .ToListAsync();
        var consultantNameById = activeConsultants.ToDictionary(c => c.Id, c => c.Name);
        var consultants = consultantRaw
            .Select(x => new ConsultantReservationStatDto(x.ConsultantId, consultantNameById.GetValueOrDefault(x.ConsultantId, ""), x.Count))
            .OrderByDescending(x => x.Count)
            .ToList();

        return Ok(new ReservationStatsDto(weekly, procedures, locales, consultants));
    }

    // 유입 경로 분석(D4·D5, 15-2절) — 어드민 전용. 클래스 레벨(Admin,HospitalManager)을 액션 레벨에서
    // 좁힌다(6-3절 원칙1과 동일 기법 — 컨트롤러 공유는 재사용, 노출 범위만 재선언).
    [HttpGet("referrals")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<ReferralStatDto>>> GetReferralStats([FromQuery] DateOnly from, [FromQuery] DateOnly to)
    {
        // 🔴 [ApiController]는 DateOnly 미지정 시 400이 아니라 default로 조용히 바인딩한다(실측 확인,
        // GetConsultantKpi와 동일 사유).
        if (from == default || to == default || to < from)
            return BadRequest(new { code = "INVALID_DATE_RANGE" });

        var (fromUtc, toUtc) = ToKstRangeUtc(from, to);

        // 1단계 — landing_daily_stats를 조합별로 집계. stat_date는 이미 KST 기준 date 컬럼이라(8-10절)
        // DateOnly로 직접 비교하면 되고 reservations처럼 UTC 환산이 필요 없다.
        var visits = await db.LandingDailyStats
            .Where(s => s.StatDate >= from && s.StatDate <= to)
            .GroupBy(s => new { s.ReferralCode, s.UtmSource, s.UtmMedium, s.UtmCampaign })
            .Select(g => new
            {
                g.Key.ReferralCode, g.Key.UtmSource, g.Key.UtmMedium, g.Key.UtmCampaign,
                VisitCount = g.Sum(s => s.VisitCount),
            })
            .ToListAsync();

        // 2단계 — 같은 조합의 reservations 집계(15-2절 "예약 수 | reservations 같은 조합 COUNT").
        var reservations = await db.Reservations
            .Where(r => r.CreatedAt >= fromUtc && r.CreatedAt < toUtc)
            .GroupBy(r => new { r.ReferralCode, r.UtmSource, r.UtmMedium, r.UtmCampaign })
            .Select(g => new
            {
                g.Key.ReferralCode, g.Key.UtmSource, g.Key.UtmMedium, g.Key.UtmCampaign,
                ReservationCount = g.Count(),
                ConfirmedCount = g.Count(r => r.Status == "Confirmed" || r.Status == "Visited"),
            })
            .ToListAsync();
        var reservationByKey = reservations.ToDictionary(x => (x.ReferralCode, x.UtmSource, x.UtmMedium, x.UtmCampaign));

        // 3단계 — 메모리에서 DTO 매핑(11-6절: GroupBy().Select(new record(...)) 직접 호출 금지와 동일 이유로
        // 집계 프로젝션은 익명 타입으로 받고 record 생성은 여기서 한다). landing_daily_stats에 없는 조합은
        // 애초에 방문 기록이 없다는 뜻이라 표에 올릴 근거가 없으므로 기준 집합에 넣지 않는다(15-2절 "그룹" 원문).
        var items = visits.Select(v =>
        {
            reservationByKey.TryGetValue((v.ReferralCode, v.UtmSource, v.UtmMedium, v.UtmCampaign), out var r);
            var reservationCount = r?.ReservationCount ?? 0;
            var confirmedCount = r?.ConfirmedCount ?? 0;
            var conversionRate = v.VisitCount == 0 ? 0m : Math.Round((decimal)reservationCount / v.VisitCount * 100, 1);
            var confirmedRate = v.VisitCount == 0 ? 0m : Math.Round((decimal)confirmedCount / v.VisitCount * 100, 1);
            return new ReferralStatDto(v.ReferralCode, v.UtmSource, v.UtmMedium, v.UtmCampaign,
                v.VisitCount, reservationCount, conversionRate, confirmedCount, confirmedRate);
        })
        .OrderByDescending(x => x.VisitCount)
        .ToList();

        return Ok(items);
    }

    // 주 시작일(일요일) 기준 집계(D16). date_trunc('week',…)는 월요일 시작이라 하루 밀어 계산 — EF Core LINQ로
    // 번역되지 않는 표현이라 이 쿼리만 raw SQL(11-4절 원문 그대로, 허용된 raw SQL 3곳 중 하나).
    private async Task<List<WeeklyReservationStatDto>> GetWeeklyStatsAsync(
        DateTimeOffset fromUtc, DateTimeOffset toUtc, DateOnly from, DateOnly to)
    {
        // 🔴 AppDbContext가 UseSnakeCaseNamingConvention()이라 SqlQuery<T>도 프로퍼티명을 스네이크케이스로
        // 변환해 컬럼을 찾는다(실측 확인 — PascalCase 따옴표 별칭 "WeekStart"는 "week_start column not found"로
        // 실패했다). 별칭은 반드시 스네이크케이스로 맞춘다.
        var rows = await db.Database.SqlQuery<WeeklyStatRow>($"""
            SELECT
              (date_trunc('week', (created_at AT TIME ZONE 'Asia/Seoul') + interval '1 day') - interval '1 day')::date AS week_start,
              COUNT(*)::int AS received,
              COUNT(*) FILTER (WHERE status IN ('Confirmed','Visited'))::int AS confirmed,
              COUNT(*) FILTER (WHERE status = 'Visited')::int AS visited,
              COUNT(*) FILTER (WHERE status = 'Cancelled')::int AS cancelled
            FROM wonjin.reservations
            WHERE deleted_at IS NULL
              AND created_at >= {fromUtc} AND created_at < {toUtc}
            GROUP BY 1
            ORDER BY 1
            """).ToListAsync();
        var byWeek = rows.ToDictionary(r => r.WeekStart);

        // 데이터 없는 주도 0으로 채운다(11-4절 완료기준) — 조회 기간의 주 시작일(일요일) 목록을 먼저 만들고 매핑.
        var result = new List<WeeklyReservationStatDto>();
        for (var week = StartOfWeek(from); week <= to; week = week.AddDays(7))
        {
            byWeek.TryGetValue(week, out var row);
            result.Add(new WeeklyReservationStatDto(week, row?.Received ?? 0, row?.Confirmed ?? 0, row?.Visited ?? 0, row?.Cancelled ?? 0));
        }
        return result;
    }

    // GetConsultantKpi·GetReservationStats 양쪽에서 쓰는 활성 실장 목록(D13 — 비활성 실장 제외).
    private async Task<List<ActiveConsultant>> GetActiveConsultantsAsync() =>
        await db.Consultants.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .Select(c => new ActiveConsultant(c.Id, c.Name))
            .ToListAsync();

    private record ActiveConsultant(int Id, string Name);

    // DayOfWeek.Sunday == 0이므로 그 값만큼 빼면 그 주의 일요일이 된다(D16 검산 — 11-4절과 동일 결과).
    private static DateOnly StartOfWeek(DateOnly d) => d.AddDays(-(int)d.DayOfWeek);

    private static (DateTimeOffset FromUtc, DateTimeOffset ToUtc) ToKstRangeUtc(DateOnly from, DateOnly to)
    {
        var fromUtc = TimeZoneInfo.ConvertTimeToUtc(from.ToDateTime(TimeOnly.MinValue), Kst);
        // 종료일 다음날 KST 00:00 미만 — 종료일 하루 전체를 포함(AdminReservationsController.GetList와 동일 관례)
        var toExclusiveUtc = TimeZoneInfo.ConvertTimeToUtc(to.AddDays(1).ToDateTime(TimeOnly.MinValue), Kst);
        return (fromUtc, toExclusiveUtc);
    }

    // db.Database.SqlQuery<T>의 T는 EF 모델에 없는 순수 결과 매핑 전용 타입 — 프로퍼티가 SELECT의 모든 컬럼과
    // 이름이 일치해야 한다(EF Core 8+ "Raw SQL queries for unmapped types", Context7 공식 문서로 확인).
    private class WeeklyStatRow
    {
        public DateOnly WeekStart { get; set; }
        public int Received { get; set; }
        public int Confirmed { get; set; }
        public int Visited { get; set; }
        public int Cancelled { get; set; }
    }
}
