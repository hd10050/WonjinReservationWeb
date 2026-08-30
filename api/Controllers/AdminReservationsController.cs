using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using WonjinApi.Data;
using WonjinApi.DTOs;
using WonjinApi.Models;
using WonjinApi.Services;
using WonjinApi.Utils;

namespace WonjinApi.Controllers;

// 조회(GET)는 3역할 전부, 쓰기(PATCH/POST/DELETE)는 Consultant·Admin만(6-3절 원칙 1·11-2절 표).
// 컨트롤러 레벨을 다중 role로 열었으므로 쓰기 액션마다 액션 레벨에서 다시 좁힌다.
[ApiController]
[Route("api/admin/reservations")]
[Authorize(Roles = "Admin,HospitalManager,Consultant")]
public class AdminReservationsController(AppDbContext db, IAdminEventBroadcaster broadcaster) : ControllerBase
{
    private static readonly TimeZoneInfo Kst = TimeZoneInfo.FindSystemTimeZoneById("Asia/Seoul");
    private const decimal MaxDepositAmount = 9999999999.99m; // DB numeric(12,2) 상한과 반드시 일치(DTO 주석 참고)

    [HttpGet]
    public async Task<ActionResult<PagedResult<ReservationListItemDto>>> GetList(
        [FromQuery] string? status,
        [FromQuery] int? consultantId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = db.Reservations.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(r => r.Status == status);
        if (consultantId.HasValue)
            query = query.Where(r => r.ConsultantId == consultantId);
        if (from.HasValue)
        {
            var fromUtc = TimeZoneInfo.ConvertTimeToUtc(from.Value.ToDateTime(TimeOnly.MinValue), Kst);
            query = query.Where(r => r.CreatedAt >= fromUtc);
        }
        if (to.HasValue)
        {
            // 종료일 다음날 KST 00:00 미만 — 종료일 하루 전체를 포함시키기 위함
            var toExclusiveUtc = TimeZoneInfo.ConvertTimeToUtc(to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), Kst);
            query = query.Where(r => r.CreatedAt < toExclusiveUtc);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = LikeEscape.EscapeContains(search);
            query = query.Where(r =>
                EF.Functions.ILike(r.Name, $"%{keyword}%", "\\")
                || EF.Functions.ILike(r.WechatId, $"%{keyword}%", "\\")
                || EF.Functions.ILike(r.Code, $"%{keyword}%", "\\"));
        }

        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(page, 1);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ReservationListItemDto(
                r.Id, r.Code, r.Name, r.WechatId, r.Status,
                r.ConsultantId, r.Consultant == null ? null : r.Consultant.Name,
                r.CreatedAt, r.VisitDate))
            .ToListAsync();

        return Ok(new PagedResult<ReservationListItemDto>(items, total, page, pageSize));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ReservationSummaryDto>> GetSummary()
    {
        // 9-2절 ③ — "이번 달"의 경계를 KST 기준으로 계산(UTC 기준이면 매월 1일 오전 9시 이전에 어긋난다)
        // ⚠️ Npgsql은 timestamptz 비교 파라미터로 Offset=0(UTC)만 허용한다 — KST(+09:00) 그대로 넘기면
        //    "Cannot write DateTimeOffset with Offset=09:00:00 ... only offset 0 (UTC) is supported" 500(실측 확인).
        var nowKst = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Kst);
        var monthStart = new DateTimeOffset(nowKst.Year, nowKst.Month, 1, 0, 0, 0, nowKst.Offset).ToUniversalTime();

        // 🔴 DB성능(2026-08-30 감사, F1) — 이전엔 GroupBy(_ => 1) + 4개 count FILTER를 한 문장으로 돌려
        // 매 대시보드 로드마다 reservations 전체 seq scan이 발생했다(WHERE가 전역 소프트삭제 필터뿐이라
        // 인덱스를 못 탐 / visited_at은 인덱스 자체가 없었음). 카드에 필요한 건 "진행 중 3개 상태 건수 +
        // 이번 달 방문완료 건수"뿐이므로 그 행만 걸러 GroupBy(status)로 집계한다 — 진행 상태 3개는
        // ix_reservations_status_created_at(status 선두 컬럼), Visited 분기는 새 부분 인덱스
        // ix_reservations_visited_at(status='Visited' 필터)로 각각 좁혀져 PG가 BitmapOr로 처리하므로
        // 전체 스캔이 사라진다. 스캔량이 테이블 크기가 아니라 "미해소 예약 수 + 이번 달 방문 수"에 비례.
        var rows = await db.Reservations
            .Where(r => r.Status == "New" || r.Status == "Consulting" || r.Status == "Confirmed"
                     || (r.Status == "Visited" && r.VisitedAt != null && r.VisitedAt >= monthStart))
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();
        var byStatus = rows.ToDictionary(x => x.Status, x => x.Count);

        // Visited 그룹은 WHERE에서 이미 "이번 달"로 걸러진 행만 담기므로 그 건수가 곧 VisitedThisMonth.
        return Ok(new ReservationSummaryDto(
            byStatus.GetValueOrDefault("New"),
            byStatus.GetValueOrDefault("Consulting"),
            byStatus.GetValueOrDefault("Confirmed"),
            byStatus.GetValueOrDefault("Visited")));
    }

    // [예약 달력] year·month는 정확히 한 달만 지정 가능 — from/to 파라미터 자체가 없어 무제한 범위
    // 조회를 클라이언트가 요청할 방법이 없다(12-6절 "최대 1개월 범위 검증"을 파라미터 설계로 만족).
    // 🔴 실제 조회 범위는 그 달의 리터럴 1일~말일이 아니라 프론트 6주(42칸) 그리드 전체다 —
    // 그래야 그리드에 걸쳐 나오는 이전달 말주·다음달 초주 셀의 예약도 표시된다(2026-08-27).
    // gridStart 계산은 calendar.vue의 gridCells와 완전히 동일해야 한다(달라지면 그리드에는
    // 보이는데 조회가 안 되는 셀이 생김). year·month가 계산의 유일한 입력이라 범위는 여전히
    // 고정 42일로 결정론적이며 클라이언트가 임의로 넓힐 수 없다.
    // 필터가 부분 인덱스 ix_reservations_visit_date의 조건(status IN ('Confirmed','Visited'))과
    // 정확히 일치해야 인덱스를 탄다(8-5절).
    // 🔴 성능(2026-08-27, "날짜 클릭 전인데 왜 다 로드돼있냐" 사용자 지적) — 이전엔 이 엔드포인트가
    // 42일치 예약 상세를 전부 반환해, 날짜를 클릭하기도 전에 한 달치 데이터가 통째로 로드돼 있었다.
    // 그리드가 실제로 표시하는 건 날짜별 배지 숫자뿐이므로 건수만 GroupBy로 집계해 반환하고,
    // 상세 목록은 GetCalendarDay로 분리해 날짜를 클릭했을 때만 그 하루치만 불러온다.
    [HttpGet("calendar")]
    public async Task<ActionResult<List<ReservationCalendarDayCountDto>>> GetCalendar([FromQuery] int year, [FromQuery] int month)
    {
        DateOnly monthStart;
        try { monthStart = new DateOnly(year, month, 1); }
        catch (ArgumentOutOfRangeException) { return BadRequest(new { code = "INVALID_CALENDAR_DATE" }); }
        var gridStart = monthStart.AddDays(-(int)monthStart.DayOfWeek); // DayOfWeek: 일요일=0
        var gridEndExclusive = gridStart.AddDays(42);

        // 🔴 GroupBy(...).Select(g => new Dto(...)) 직결 금지(11-6절 함정) — EF Core가 SQL로 못 옮겨
        // 런타임 예외. 익명 타입으로 먼저 집계한 뒤 메모리에서 DTO로 매핑하는 2단계로 우회한다.
        var rows = await db.Reservations
            .Where(r => r.VisitDate != null && r.VisitDate >= gridStart && r.VisitDate < gridEndExclusive
                     && (r.Status == "Confirmed" || r.Status == "Visited"))
            .GroupBy(r => r.VisitDate!.Value)
            .Select(g => new { VisitDate = g.Key, Count = g.Count() })
            .ToListAsync();

        var counts = rows.Select(r => new ReservationCalendarDayCountDto(r.VisitDate, r.Count)).ToList();
        return Ok(counts);
    }

    // 위 GetCalendar가 반환하던 상세 목록을 여기로 분리(2026-08-27) — 날짜 클릭 시에만 그 하루치를
    // 불러온다. 단일 날짜 + 동일 status 필터라 GetCalendar와 같은 부분 인덱스(ix_reservations_visit_date)를
    // 그대로 탄다. date는 [FromQuery] DateOnly(비-nullable) — 파라미터 누락 시 400이 아니라 default
    // (0001-01-01)로 조용히 바인딩되는 함정(11-8절)이 있어 명시적으로 검사한다.
    [HttpGet("calendar/day")]
    public async Task<ActionResult<List<ReservationCalendarItemDto>>> GetCalendarDay([FromQuery] DateOnly date)
    {
        if (date == default) return BadRequest(new { code = "INVALID_CALENDAR_DATE" });

        var items = await db.Reservations
            .Where(r => r.VisitDate == date && (r.Status == "Confirmed" || r.Status == "Visited"))
            .OrderBy(r => r.VisitTime)
            .Select(r => new ReservationCalendarItemDto(
                r.Id, r.Code, r.Name, r.Status, r.VisitDate!.Value, r.VisitTime,
                r.Consultant == null ? null : r.Consultant.Name))
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReservationDetailDto>> GetDetail(int id)
    {
        var r = await db.Reservations
            .Include(x => x.Consultant)
            .Include(x => x.ReservationProcedures)
            .Include(x => x.Notes)
            .Include(x => x.Logs)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (r is null) return NotFound();

        var notes = r.Notes.OrderBy(n => n.CreatedAt)
            .Select(n => new ReservationNoteDto(n.Id, n.Body, n.AuthorUserId, n.AuthorName, n.CreatedAt, n.UpdatedAt, n.UpdatedAt != n.CreatedAt))
            .ToList();
        var logs = r.Logs.OrderBy(l => l.CreatedAt)
            .Select(l => new ReservationLogDto(l.Id, l.Action, l.Note, l.ActorName, l.CreatedAt))
            .ToList();

        return Ok(new ReservationDetailDto(
            r.Id, r.Code, r.Name, r.BirthDate, r.Gender, r.WechatId, r.PreferredContactDate, r.PreferredContactTime, r.Locale, r.Status,
            r.ConsultantId, r.Consultant?.Name,
            r.VisitDate, r.VisitTime, r.DepositAmount, r.DepositCurrency, r.DepositPaid, r.CancelReason,
            r.UtmSource, r.UtmMedium, r.UtmCampaign, r.ReferralCode,
            r.CreatedAt, r.UpdatedAt, r.ConsultingAt, r.ConfirmedAt, r.VisitedAt, r.CancelledAt,
            r.ReservationProcedures.Select(rp => rp.ProcedureId).ToArray(),
            notes, logs));
    }

    // 방문일시·시술·예약금 저장. 미배정이면 400(D17, 10-1절). 취소·방문완료 상태도 400(11-2절 잠금).
    [HttpPatch("{id:int}")]
    [Authorize(Roles = "Admin,Consultant")]
    [EnableRateLimiting("admin-write")]
    public async Task<ActionResult<ReservationDetailDto>> UpdateReservation(int id, [FromBody] UpdateReservationRequest req)
    {
        if (req.DepositCurrency is not ("CNY" or "KRW"))
            return BadRequest(new { code = "INVALID_DEPOSIT_CURRENCY" });

        // 🔴 [Range] 대신 수동 검증(ReservationDto.cs 주석 참고) — 응답을 앱 공용 {code} 형식으로 맞춰
        // 프론트가 "알 수 없는 오류" 대신 정확한 안내를 보여주게 한다. 음수·numeric overflow 둘 다 여기서 막는다.
        if (req.DepositAmount is < 0 or > MaxDepositAmount)
            return BadRequest(new { code = "INVALID_DEPOSIT_AMOUNT" });

        // 🔴 시술 ID 존재 검증 — AssignConsultant의 CONSULTANT_NOT_FOUND와 대칭. 검증 없이 그대로 삽입하면
        // FK 위반(fk_reservation_procedures_procedures_procedure_id)이 SaveChangesAsync에서 500으로
        // 터지고, 트랜잭션이 없던 예전 코드에서는 그 시점에 이미 스칼라 필드·자동전이가 커밋된 뒤라
        // "응답은 실패인데 시술 목록만 조용히 삭제된" 상태가 됐다(실측 확인 — 재감사 1번 결함).
        var distinctProcedureIds = req.ProcedureIds.Distinct().ToArray();
        if (distinctProcedureIds.Length > 0)
        {
            var existingCount = await db.Procedures.CountAsync(p => distinctProcedureIds.Contains(p.Id));
            if (existingCount != distinctProcedureIds.Length)
                return BadRequest(new { code = "INVALID_PROCEDURE_IDS" });
        }

        var before = await db.Reservations.AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new { r.ConsultantId, r.Status, r.VisitDate, r.VisitTime, r.DepositAmount, r.DepositCurrency, r.DepositPaid })
            .FirstOrDefaultAsync();
        if (before is null) return NotFound();
        if (before.ConsultantId is null) return BadRequest(new { code = "RESERVATION_NOT_ASSIGNED" });
        // 🔴 취소·방문완료는 담당 실장 배정 전과 동일하게 잠긴다 — 서버가 실제 방어선(11-2절).
        if (before.Status is "Cancelled" or "Visited") return BadRequest(new { code = "RESERVATION_LOCKED" });

        var beforeProcedureIds = await db.ReservationProcedures.AsNoTracking()
            .Where(rp => rp.ReservationId == id)
            .Select(rp => rp.ProcedureId)
            .ToListAsync();

        var now = DateTimeOffset.UtcNow;

        // 🔴 스칼라 저장·시술 재설정·자동전이·로그 기록을 하나의 트랜잭션으로 묶는다 — 그중 하나라도
        // 실패하면 전부 롤백되어 "응답은 실패인데 일부만 반영된" 상태를 만들지 않는다(재감사 1번 결함 수정).
        await using var tx = await db.Database.BeginTransactionAsync();

        // D17·잠금 상태 둘 다 같은 UPDATE의 WHERE에 다시 넣어 조회~쓰기 사이 변경된 경우를 닫는다(10-1절).
        var affected = await db.Reservations
            .Where(r => r.Id == id && r.ConsultantId != null && r.Status != "Cancelled" && r.Status != "Visited")
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.VisitDate, req.VisitDate)
                .SetProperty(r => r.VisitTime, req.VisitTime)
                .SetProperty(r => r.DepositAmount, req.DepositAmount)
                .SetProperty(r => r.DepositCurrency, req.DepositCurrency)
                .SetProperty(r => r.DepositPaid, req.DepositPaid)
                .SetProperty(r => r.UpdatedAt, now));

        if (affected == 0)
            return await DiagnoseWriteFailureAsync(id); // tx는 커밋 안 됐으므로 using 종료 시 자동 롤백

        await db.ReservationProcedures.Where(rp => rp.ReservationId == id).ExecuteDeleteAsync();
        if (distinctProcedureIds.Length > 0)
        {
            db.ReservationProcedures.AddRange(
                distinctProcedureIds.Select(pid => new ReservationProcedure { ReservationId = id, ProcedureId = pid }));
        }

        // New/Consulting에서 방문일이 정해지면 Confirmed로 자동 전이(10장) — 예약금 확인 여부는 더 이상
        // 조건이 아니다(2026-08-27, 예약금 미확인 상태에서도 내원 확인이 가능해야 한다는 요구로 완화.
        // 예약금 확인 자체는 아래에서 별도로 계속 추적·기록된다).
        var confirmedAffected = 0;
        if (req.VisitDate is not null)
        {
            confirmedAffected = await db.Reservations
                .Where(r => r.Id == id && (r.Status == "New" || r.Status == "Consulting"))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.Status, "Confirmed")
                    .SetProperty(r => r.ConfirmedAt, now)
                    .SetProperty(r => r.UpdatedAt, now));
        }

        var (userId, userName) = await GetCurrentUserAsync();

        // 🔴 예약금·방문일시·시술 — 실제로 값이 바뀐 항목만 처리 이력에 남긴다(전량 이력화 요구,
        // "저장" 버튼을 값 변경 없이 눌러도 로그가 쌓이지 않도록 비교 후에만 기록).
        var depositBecamePaid = req.DepositPaid && !before.DepositPaid;
        var depositBecameUnpaid = !req.DepositPaid && before.DepositPaid;
        var depositAmountOrCurrencyChanged = before.DepositAmount != req.DepositAmount || before.DepositCurrency != req.DepositCurrency;

        if (depositBecamePaid)
        {
            // 예약금 없음(면제) 라디오도 내부적으로는 DepositPaid=true로 처리한다(#13) — 금액 유무로 문구만 구분.
            var reason = req.DepositAmount is null
                ? "예약금 없음(입금 불필요 처리)"
                : $"{FormatDeposit(req.DepositAmount, req.DepositCurrency)} 입금 확인";
            db.ReservationLogs.Add(new ReservationLog { ReservationId = id, Action = "deposit_confirmed", Note = Cap(reason), ActorUserId = userId, ActorName = userName, CreatedAt = now });
        }
        else if (depositAmountOrCurrencyChanged || depositBecameUnpaid)
        {
            var parts = new List<string>();
            if (depositAmountOrCurrencyChanged)
                parts.Add($"예약금 {FormatDeposit(before.DepositAmount, before.DepositCurrency)} → {FormatDeposit(req.DepositAmount, req.DepositCurrency)}");
            if (depositBecameUnpaid)
                parts.Add("입금 확인 해제");
            db.ReservationLogs.Add(new ReservationLog { ReservationId = id, Action = "deposit_updated", Note = Cap(string.Join(", ", parts)), ActorUserId = userId, ActorName = userName, CreatedAt = now });
        }

        if (before.VisitDate != req.VisitDate || before.VisitTime != req.VisitTime)
        {
            string FormatSchedule(DateOnly? d, TimeOnly? t) =>
                d is null ? "미정" : $"{d.Value:yyyy-MM-dd} {t?.ToString("HH:mm") ?? ""}".TrimEnd();
            db.ReservationLogs.Add(new ReservationLog
            {
                ReservationId = id,
                Action = "visit_schedule_changed",
                Note = Cap($"방문일시 {FormatSchedule(before.VisitDate, before.VisitTime)} → {FormatSchedule(req.VisitDate, req.VisitTime)}"),
                ActorUserId = userId, ActorName = userName, CreatedAt = now,
            });
        }

        var beforeProcSet = beforeProcedureIds.ToHashSet();
        var afterProcSet = distinctProcedureIds.ToHashSet();
        if (!beforeProcSet.SetEquals(afterProcSet))
        {
            var unionIds = beforeProcSet.Union(afterProcSet).ToArray();
            var nameMap = await db.Procedures.AsNoTracking()
                .Where(p => unionIds.Contains(p.Id))
                .Select(p => new { p.Id, p.NameKo })
                .ToDictionaryAsync(p => p.Id, p => p.NameKo);
            string Names(IEnumerable<int> ids)
            {
                var joined = string.Join(", ", ids.Select(pid => nameMap.GetValueOrDefault(pid, $"#{pid}")));
                return joined.Length == 0 ? "없음" : joined;
            }
            db.ReservationLogs.Add(new ReservationLog
            {
                ReservationId = id,
                Action = "procedure_changed",
                Note = Cap($"시술 {Names(beforeProcSet)} → {Names(afterProcSet)}"),
                ActorUserId = userId, ActorName = userName, CreatedAt = now,
            });
        }

        if (confirmedAffected > 0)
            db.ReservationLogs.Add(new ReservationLog { ReservationId = id, Action = "status_changed", Note = "방문일 확정 → Confirmed", ActorUserId = userId, ActorName = userName, CreatedAt = now });

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        // 커밋 성공 후에만 발행 — 롤백될 수도 있는 변경을 미리 알리면 안 된다(2026-08-27, SSE 조용한 새로고침).
        if (confirmedAffected > 0)
            broadcaster.PublishReservationConfirmed(id);

        return await GetDetail(id);
    }

    // 담당 실장 배정·변경 전용. 미배정 상태에서도 허용되는 유일한 쓰기다(D17) — 처리 이력 필수 기록.
    [HttpPatch("{id:int}/consultant")]
    [Authorize(Roles = "Admin,Consultant")]
    [EnableRateLimiting("admin-write")]
    public async Task<ActionResult<ReservationDetailDto>> AssignConsultant(int id, [FromBody] AssignConsultantRequest req)
    {
        var consultant = await db.Consultants.AsNoTracking().FirstOrDefaultAsync(c => c.Id == req.ConsultantId);
        if (consultant is null) return BadRequest(new { code = "CONSULTANT_NOT_FOUND" });
        // 🔴 web-security-audit-guide.md 17장 재감사(2026-08-27) 발견 — 비활성 실장은 신규 배정
        // 드롭다운에서만 제외됐고(D13, 화면 UX) 서버는 존재 여부만 검사해 API 직접 호출로 비활성
        // 실장에게도 배정이 가능했다. D17("화면 비활성화는 UX일 뿐, 실제 차단은 서버가 한다")과
        // 동일 원칙을 여기도 적용.
        if (!consultant.IsActive) return BadRequest(new { code = "CONSULTANT_INACTIVE" });

        var now = DateTimeOffset.UtcNow;
        var (userId, userName) = await GetCurrentUserAsync();

        // 🔴 보안감사(2026-08-26) 1차 완화(트랜잭션 통일)에 이어 2026-08-27 RowVersion(xmin) 낙관적
        // 동시성 토큰으로 완전 해결. 조회 시점의 RowVersion을 조건부 UPDATE의 WHERE에 함께 걸어,
        // 조회~반영 사이 다른 요청이 먼저 커밋되면 이 UPDATE는 0행 매치로 실패 → 409 반환. 이제
        // 로그의 "이전 담당자명"이 항상 실제 직전 값과 일치함을 DB 레벨에서 보장한다(더 이상
        // "최종 상태는 정확하지만 로그가 부정확할 수 있음"이 아니다).
        await using var tx = await db.Database.BeginTransactionAsync();

        var before = await db.Reservations.AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new { r.ConsultantId, r.Status, r.RowVersion })
            .FirstOrDefaultAsync();
        if (before is null) return NotFound();
        // 🔴 취소·방문완료는 담당 실장 배정 전과 동일하게 잠긴다 — 재배정도 예외 없다(11-2절).
        if (before.Status is "Cancelled" or "Visited") return BadRequest(new { code = "RESERVATION_LOCKED" });

        var prevName = "미배정";
        if (before.ConsultantId is not null)
        {
            prevName = await db.Consultants.AsNoTracking()
                .Where(c => c.Id == before.ConsultantId)
                .Select(c => c.Name)
                .FirstOrDefaultAsync() ?? "알 수 없음";
        }

        var affected = await db.Reservations
            .Where(r => r.Id == id && r.RowVersion == before.RowVersion && r.Status != "Cancelled" && r.Status != "Visited")
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.ConsultantId, req.ConsultantId)
                .SetProperty(r => r.UpdatedAt, now));

        if (affected == 0)
            return Conflict(new { code = "RESERVATION_STATE_CHANGED" }); // tx 미커밋 → using 종료 시 자동 롤백

        db.ReservationLogs.Add(new ReservationLog
        {
            ReservationId = id,
            Action = "assigned",
            Note = $"{prevName} → {consultant.Name}",
            ActorUserId = userId,
            ActorName = userName,
            CreatedAt = now,
        });

        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return await GetDetail(id);
    }

    // 상태 전이(10장). Confirmed→Visited 또는 (New|Consulting|Confirmed)→Cancelled만 허용. 미배정이면 400(D17).
    [HttpPost("{id:int}/status")]
    [Authorize(Roles = "Admin,Consultant")]
    [EnableRateLimiting("admin-write")]
    public async Task<ActionResult<ReservationDetailDto>> ChangeStatus(int id, [FromBody] ChangeStatusRequest req)
    {
        var now = DateTimeOffset.UtcNow;
        int affected;
        string logAction;
        string? logNote;

        // 🔴 상태 전이 UPDATE와 그 처리 이력 기록을 하나의 트랜잭션으로 묶는다 — 로그 기록이 실패해도
        // 이미 커밋된 상태 전이만 남고 응답은 실패로 보이는 불일치를 막는다(재감사 1번 결함과 동일 패턴).
        await using var tx = await db.Database.BeginTransactionAsync();

        if (req.Status == "Visited")
        {
            affected = await db.Reservations
                .Where(r => r.Id == id && r.ConsultantId != null && r.Status == "Confirmed")
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.Status, "Visited")
                    .SetProperty(r => r.VisitedAt, now)
                    .SetProperty(r => r.UpdatedAt, now));
            logAction = "status_changed";
            logNote = "Confirmed → Visited";
        }
        else if (req.Status == "Cancelled")
        {
            if (string.IsNullOrWhiteSpace(req.CancelReason))
                return BadRequest(new { code = "CANCEL_REASON_REQUIRED" });

            // 🔴 취소는 미배정 상태에서도 허용한다(2026-08-27 요구 변경 — 하드 삭제를 없애고 미배정
            // 예약의 정리 수단을 삭제 대신 취소로 통일했다) — ConsultantId 조건을 의도적으로 뺀다.
            affected = await db.Reservations
                .Where(r => r.Id == id
                         && (r.Status == "New" || r.Status == "Consulting" || r.Status == "Confirmed"))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.Status, "Cancelled")
                    .SetProperty(r => r.CancelledAt, now)
                    .SetProperty(r => r.CancelReason, req.CancelReason)
                    .SetProperty(r => r.UpdatedAt, now));
            logAction = "cancelled";
            logNote = req.CancelReason;
        }
        else
        {
            return BadRequest(new { code = "INVALID_STATUS_TRANSITION" });
        }

        if (affected == 0)
            return await DiagnoseWriteFailureAsync(id, requiresAssignment: req.Status != "Cancelled");

        var (userId, userName) = await GetCurrentUserAsync();
        db.ReservationLogs.Add(new ReservationLog
        {
            ReservationId = id, Action = logAction, Note = logNote, ActorUserId = userId, ActorName = userName, CreatedAt = now,
        });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return await GetDetail(id);
    }

    // 상담 기록 추가(누적, D14). 미배정이면 400(D17). 최초 기록이면 New→Consulting 자동 전이(10장).
    [HttpPost("{id:int}/notes")]
    [Authorize(Roles = "Admin,Consultant")]
    [EnableRateLimiting("admin-write")]
    public async Task<ActionResult<ReservationNoteDto>> AddNote(int id, [FromBody] AddNoteRequest req)
    {
        var now = DateTimeOffset.UtcNow;
        var (userId, userName) = await GetCurrentUserAsync();

        // 🔴 상담기록 추가 + 자동전이 + 두 로그 기록을 하나의 트랜잭션으로 묶는다 — 뒤쪽 SaveChangesAsync가
        // 실패해도 앞서 저장된 상담기록까지 롤백되어 부분 반영을 막는다(재감사 1번 결함과 동일 패턴).
        await using var tx = await db.Database.BeginTransactionAsync();

        // 🔴 보안감사(2026-08-26) 발견 — 이전엔 배정 여부를 트랜잭션 밖 별도 SELECT로만 확인했다.
        // ExecuteUpdateAsync 조건부 UPDATE가 이 행에 row-level lock을 걸어, 동시에 들어온 다른 쓰기의
        // UPDATE와 서로 직렬화되게 한다 — 상대가 먼저 커밋되면 아래 WHERE 조건에 안 걸려 touched=0이
        // 되어 안전하게 막힌다(경쟁하는 대상은 원래 SoftDelete/D15였으나, 2026-08-27 소프트 삭제
        // 기능 자체가 폐지되어 지금은 예약 취소(Cancelled 전이)가 그 자리를 대신한다 — 아래 참고).
        // 🔴 취소된 예약은 상담 기록 추가도 잠근다(11-2절) — 방문완료는 예외(#14, 사후 상담 기록 목적).
        var touched = await db.Reservations
            .Where(r => r.Id == id && r.ConsultantId != null && r.Status != "Cancelled")
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.UpdatedAt, now));
        if (touched == 0)
            return await DiagnoseNoteWriteFailureAsync(id);

        var note = new ReservationNote
        {
            ReservationId = id, Body = req.Body, AuthorUserId = userId, AuthorName = userName, CreatedAt = now, UpdatedAt = now,
        };
        db.ReservationNotes.Add(note);
        db.ReservationLogs.Add(new ReservationLog { ReservationId = id, Action = "note_added", ActorUserId = userId, ActorName = userName, CreatedAt = now });
        await db.SaveChangesAsync();

        // 배정만으로는 전이하지 않는다 — 최초 상담 기록이 New→Consulting의 유일한 트리거(10장)
        var promoted = await db.Reservations
            .Where(r => r.Id == id && r.Status == "New")
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.Status, "Consulting")
                .SetProperty(r => r.ConsultingAt, now)
                .SetProperty(r => r.UpdatedAt, now));
        if (promoted > 0)
        {
            db.ReservationLogs.Add(new ReservationLog { ReservationId = id, Action = "status_changed", Note = "New → Consulting", ActorUserId = userId, ActorName = userName, CreatedAt = now });
            await db.SaveChangesAsync();
        }

        await tx.CommitAsync();

        return Ok(new ReservationNoteDto(note.Id, note.Body, note.AuthorUserId, note.AuthorName, note.CreatedAt, note.UpdatedAt, false));
    }

    // 상담 기록 수정. 작성자 본인 또는 Admin만 — 삭제 엔드포인트는 만들지 않는다(D14).
    // 🔴 수정 전 본문을 revisions에 스냅샷으로 남긴다(수정 이력 모달용) + 처리 이력에도 기록한다.
    [HttpPatch("{id:int}/notes/{noteId:int}")]
    [Authorize(Roles = "Admin,Consultant")]
    [EnableRateLimiting("admin-write")]
    public async Task<ActionResult<ReservationNoteDto>> UpdateNote(int id, int noteId, [FromBody] UpdateNoteRequest req)
    {
        var note = await db.ReservationNotes.FirstOrDefaultAsync(n => n.Id == noteId && n.ReservationId == id);
        if (note is null) return NotFound();

        var role = User.FindFirstValue(ClaimTypes.Role);
        var (userId, userName) = await GetCurrentUserAsync();
        if (role != "Admin" && note.AuthorUserId != userId)
            return Forbid();

        // 🔴 취소된 예약은 상담 기록 수정도 잠근다(11-2절) — 방문완료는 예외(#14, 사후 상담 기록 목적).
        var status = await db.Reservations.AsNoTracking().Where(r => r.Id == id).Select(r => r.Status).FirstOrDefaultAsync();
        if (status == "Cancelled") return BadRequest(new { code = "RESERVATION_LOCKED" });

        var now = DateTimeOffset.UtcNow;
        db.ReservationNoteRevisions.Add(new ReservationNoteRevision
        {
            ReservationNoteId = note.Id, Body = note.Body, EditedByUserId = userId, EditedByName = userName, EditedAt = now,
        });
        note.Body = req.Body;
        note.UpdatedAt = now;
        db.ReservationLogs.Add(new ReservationLog { ReservationId = id, Action = "note_updated", ActorUserId = userId, ActorName = userName, CreatedAt = now });
        await db.SaveChangesAsync();

        return Ok(new ReservationNoteDto(note.Id, note.Body, note.AuthorUserId, note.AuthorName, note.CreatedAt, note.UpdatedAt, true));
    }

    // 상담 기록 수정 이력 조회(#5) — 조회는 3역할 전부(클래스 레벨 Authorize로 이미 충분).
    [HttpGet("{id:int}/notes/{noteId:int}/revisions")]
    public async Task<ActionResult<List<ReservationNoteRevisionDto>>> GetNoteRevisions(int id, int noteId)
    {
        var noteExists = await db.ReservationNotes.AsNoTracking().AnyAsync(n => n.Id == noteId && n.ReservationId == id);
        if (!noteExists) return NotFound();

        var revisions = await db.ReservationNoteRevisions.AsNoTracking()
            .Where(r => r.ReservationNoteId == noteId)
            .OrderByDescending(r => r.EditedAt)
            .Select(r => new ReservationNoteRevisionDto(r.Id, r.Body, r.EditedByName, r.EditedAt))
            .ToListAsync();

        return Ok(revisions);
    }

    // 취소된 예약을 되돌린다(#10). 어드민만 — 종결 상태를 되돌리는 액션은 별도 승인 주체를 둔다는
    // 기존 설계 방향(10-1절 "되돌리기가 필요하면 어드민만")을 그대로 따른다.
    [HttpPost("{id:int}/restore")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("admin-write")]
    public async Task<ActionResult<ReservationDetailDto>> RestoreCancelled(int id)
    {
        var info = await db.Reservations.AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new { r.Status, r.VisitDate, HasNotes = r.Notes.Any() })
            .FirstOrDefaultAsync();
        if (info is null) return NotFound();
        if (info.Status != "Cancelled") return BadRequest(new { code = "RESERVATION_NOT_CANCELLED" });

        // 취소 시점 데이터(방문일·상담기록)가 그대로 남아있으므로, 순방향 전이 규칙(10장)과 동일한
        // 기준으로 되돌아갈 상태를 계산한다 — 무조건 New로 되돌리면 이미 진행된 상담·확정 이력이 사라져 보인다.
        var targetStatus = info.VisitDate is not null ? "Confirmed" : info.HasNotes ? "Consulting" : "New";

        var now = DateTimeOffset.UtcNow;
        await using var tx = await db.Database.BeginTransactionAsync();

        var affected = await db.Reservations
            .Where(r => r.Id == id && r.Status == "Cancelled")
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.Status, targetStatus)
                .SetProperty(r => r.CancelledAt, (DateTimeOffset?)null)
                .SetProperty(r => r.CancelReason, (string?)null)
                .SetProperty(r => r.UpdatedAt, now));

        if (affected == 0)
            return Conflict(new { code = "RESERVATION_STATE_CHANGED" });

        var (userId, userName) = await GetCurrentUserAsync();
        db.ReservationLogs.Add(new ReservationLog { ReservationId = id, Action = "restored", Note = $"Cancelled → {targetStatus}", ActorUserId = userId, ActorName = userName, CreatedAt = now });
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return await GetDetail(id);
    }

    // affected==0의 이유가 셋(없음/미배정/상태변경됨)이라 구분해서 응답해야 화면이 올바른 안내를 띄운다(10-1절).
    // requiresAssignment=false는 취소처럼 미배정 상태에서도 허용되는 전이 실패를 진단할 때 쓴다 — 그 경우
    // ConsultantId가 null이어도 "미배정" 때문이 아니라 상태가 이미 바뀐 것이 원인이므로 이 인자로 구분한다.
    private async Task<ActionResult<ReservationDetailDto>> DiagnoseWriteFailureAsync(int id, bool requiresAssignment = true)
    {
        var cur = await db.Reservations.AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new { r.Status, r.ConsultantId })
            .FirstOrDefaultAsync();

        if (cur is null) return NotFound();
        if (requiresAssignment && cur.ConsultantId is null) return BadRequest(new { code = "RESERVATION_NOT_ASSIGNED" });
        return Conflict(new { code = "RESERVATION_STATE_CHANGED" });
    }

    // AddNote 전용 — 판정 로직은 DiagnoseWriteFailureAsync와 같지만 반환 DTO 타입이 다르다(ReservationNoteDto).
    private async Task<ActionResult<ReservationNoteDto>> DiagnoseNoteWriteFailureAsync(int id)
    {
        var cur = await db.Reservations.AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new { r.ConsultantId })
            .FirstOrDefaultAsync();

        if (cur is null) return NotFound();
        if (cur.ConsultantId is null) return BadRequest(new { code = "RESERVATION_NOT_ASSIGNED" });
        return Conflict(new { code = "RESERVATION_STATE_CHANGED" });
    }

    private async Task<(int UserId, string UserName)> GetCurrentUserAsync()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        int.TryParse(userIdStr, out var userId);
        var name = await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.Name).FirstOrDefaultAsync() ?? "SYSTEM";
        return (userId, name);
    }

    private static string FormatDeposit(decimal? amount, string currency) =>
        amount is null ? "없음" : $"{amount:0.##} {currency}";

    // reservation_logs.note는 varchar(300) — 시술명 여러 개가 합쳐지는 등 드물게 넘칠 수 있어 방어적으로 자른다.
    private static string Cap(string s) => s.Length <= 300 ? s : s[..297] + "...";
}
