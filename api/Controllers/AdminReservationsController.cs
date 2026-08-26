using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WonjinApi.Data;
using WonjinApi.DTOs;
using WonjinApi.Models;

namespace WonjinApi.Controllers;

// 조회(GET)는 3역할 전부, 쓰기(PATCH/POST/DELETE)는 Consultant·Admin만(6-3절 원칙 1·11-2절 표).
// 컨트롤러 레벨을 다중 role로 열었으므로 쓰기 액션마다 액션 레벨에서 다시 좁힌다.
[ApiController]
[Route("api/admin/reservations")]
[Authorize(Roles = "Admin,HospitalManager,Consultant")]
public class AdminReservationsController(AppDbContext db, ILogger<AdminReservationsController> logger) : ControllerBase
{
    private static readonly TimeZoneInfo Kst = TimeZoneInfo.FindSystemTimeZoneById("Asia/Seoul");

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
            var keyword = EscapeLike(search.Trim());
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

        var summary = await db.Reservations
            .GroupBy(_ => 1)
            .Select(g => new
            {
                New = g.Count(r => r.Status == "New"),
                Consulting = g.Count(r => r.Status == "Consulting"),
                Confirmed = g.Count(r => r.Status == "Confirmed"),
                VisitedThisMonth = g.Count(r => r.Status == "Visited" && r.VisitedAt != null && r.VisitedAt >= monthStart),
            })
            .FirstOrDefaultAsync();

        return Ok(new ReservationSummaryDto(
            summary?.New ?? 0, summary?.Consulting ?? 0, summary?.Confirmed ?? 0, summary?.VisitedThisMonth ?? 0));
    }

    // [예약 달력] year·month는 정확히 한 달만 조회 가능 — from/to 파라미터 자체가 없어 무제한 범위
    // 조회를 클라이언트가 요청할 방법이 없다(12-6절 "최대 1개월 범위 검증"을 파라미터 설계로 만족).
    // 필터가 부분 인덱스 ix_reservations_visit_date의 조건(status IN ('Confirmed','Visited'))과
    // 정확히 일치해야 인덱스를 탄다(8-5절).
    [HttpGet("calendar")]
    public async Task<ActionResult<List<ReservationCalendarItemDto>>> GetCalendar([FromQuery] int year, [FromQuery] int month)
    {
        DateOnly monthStart;
        try { monthStart = new DateOnly(year, month, 1); }
        catch (ArgumentOutOfRangeException) { return BadRequest(new { code = "INVALID_CALENDAR_DATE" }); }
        var monthEndExclusive = monthStart.AddMonths(1);

        var items = await db.Reservations
            .Where(r => r.VisitDate != null && r.VisitDate >= monthStart && r.VisitDate < monthEndExclusive
                     && (r.Status == "Confirmed" || r.Status == "Visited"))
            .OrderBy(r => r.VisitDate).ThenBy(r => r.VisitTime)
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
            r.Id, r.Code, r.Name, r.BirthDate, r.Gender, r.WechatId, r.PreferredContactTime, r.Locale, r.Status,
            r.ConsultantId, r.Consultant?.Name,
            r.VisitDate, r.VisitTime, r.DepositAmount, r.DepositCurrency, r.DepositPaid, r.CancelReason,
            r.UtmSource, r.UtmMedium, r.UtmCampaign, r.ReferralCode,
            r.CreatedAt, r.UpdatedAt, r.ConsultingAt, r.ConfirmedAt, r.VisitedAt, r.CancelledAt,
            r.ReservationProcedures.Select(rp => rp.ProcedureId).ToArray(),
            notes, logs));
    }

    // 방문일시·시술·예약금 저장. 미배정이면 400(D17, 10-1절).
    [HttpPatch("{id:int}")]
    [Authorize(Roles = "Admin,Consultant")]
    public async Task<ActionResult<ReservationDetailDto>> UpdateReservation(int id, [FromBody] UpdateReservationRequest req)
    {
        if (req.DepositCurrency is not ("CNY" or "KRW"))
            return BadRequest(new { code = "INVALID_DEPOSIT_CURRENCY" });

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
            .Select(r => new { r.ConsultantId, r.DepositPaid })
            .FirstOrDefaultAsync();
        if (before is null) return NotFound();
        if (before.ConsultantId is null) return BadRequest(new { code = "RESERVATION_NOT_ASSIGNED" });

        var now = DateTimeOffset.UtcNow;

        // 🔴 스칼라 저장·시술 재설정·자동전이·로그 기록을 하나의 트랜잭션으로 묶는다 — 그중 하나라도
        // 실패하면 전부 롤백되어 "응답은 실패인데 일부만 반영된" 상태를 만들지 않는다(재감사 1번 결함 수정).
        await using var tx = await db.Database.BeginTransactionAsync();

        // D17 — 배정 여부를 같은 UPDATE의 WHERE에 다시 넣어 조회~쓰기 사이 배정 해제된 경우를 닫는다(10-1절).
        var affected = await db.Reservations
            .Where(r => r.Id == id && r.ConsultantId != null)
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

        // New/Consulting에서 방문일+입금확인 둘 다 충족하면 Confirmed로 자동 전이(10장)
        var confirmedAffected = 0;
        if (req.VisitDate is not null && req.DepositPaid)
        {
            confirmedAffected = await db.Reservations
                .Where(r => r.Id == id && (r.Status == "New" || r.Status == "Consulting"))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.Status, "Confirmed")
                    .SetProperty(r => r.ConfirmedAt, now)
                    .SetProperty(r => r.UpdatedAt, now));
        }

        var (userId, userName) = await GetCurrentUserAsync();
        var depositNewlyConfirmed = req.DepositPaid && !before.DepositPaid;
        if (depositNewlyConfirmed)
            db.ReservationLogs.Add(new ReservationLog { ReservationId = id, Action = "deposit_confirmed", ActorUserId = userId, ActorName = userName, CreatedAt = now });
        if (confirmedAffected > 0)
            db.ReservationLogs.Add(new ReservationLog { ReservationId = id, Action = "status_changed", Note = "예약금·방문일 확인 → Confirmed", ActorUserId = userId, ActorName = userName, CreatedAt = now });

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return await GetDetail(id);
    }

    // 담당 실장 배정·변경 전용. 미배정 상태에서도 허용되는 유일한 쓰기다(D17) — 처리 이력 필수 기록.
    [HttpPatch("{id:int}/consultant")]
    [Authorize(Roles = "Admin,Consultant")]
    public async Task<ActionResult<ReservationDetailDto>> AssignConsultant(int id, [FromBody] AssignConsultantRequest req)
    {
        var reservation = await db.Reservations.FirstOrDefaultAsync(r => r.Id == id);
        if (reservation is null) return NotFound();

        var consultant = await db.Consultants.AsNoTracking().FirstOrDefaultAsync(c => c.Id == req.ConsultantId);
        if (consultant is null) return BadRequest(new { code = "CONSULTANT_NOT_FOUND" });

        var prevName = "미배정";
        if (reservation.ConsultantId is not null)
        {
            prevName = await db.Consultants.AsNoTracking()
                .Where(c => c.Id == reservation.ConsultantId)
                .Select(c => c.Name)
                .FirstOrDefaultAsync() ?? "알 수 없음";
        }

        var now = DateTimeOffset.UtcNow;
        reservation.ConsultantId = req.ConsultantId;
        reservation.UpdatedAt = now;

        var (userId, userName) = await GetCurrentUserAsync();
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
        return await GetDetail(id);
    }

    // 상태 전이(10장). Confirmed→Visited 또는 (New|Consulting|Confirmed)→Cancelled만 허용. 미배정이면 400(D17).
    [HttpPost("{id:int}/status")]
    [Authorize(Roles = "Admin,Consultant")]
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

            affected = await db.Reservations
                .Where(r => r.Id == id && r.ConsultantId != null
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
            return await DiagnoseWriteFailureAsync(id);

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
    public async Task<ActionResult<ReservationNoteDto>> AddNote(int id, [FromBody] AddNoteRequest req)
    {
        var reservation = await db.Reservations.AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new { r.ConsultantId })
            .FirstOrDefaultAsync();
        if (reservation is null) return NotFound();
        if (reservation.ConsultantId is null) return BadRequest(new { code = "RESERVATION_NOT_ASSIGNED" });

        var now = DateTimeOffset.UtcNow;
        var (userId, userName) = await GetCurrentUserAsync();

        // 🔴 상담기록 추가 + 자동전이 + 두 로그 기록을 하나의 트랜잭션으로 묶는다 — 뒤쪽 SaveChangesAsync가
        // 실패해도 앞서 저장된 상담기록까지 롤백되어 부분 반영을 막는다(재감사 1번 결함과 동일 패턴).
        await using var tx = await db.Database.BeginTransactionAsync();

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
    [HttpPatch("{id:int}/notes/{noteId:int}")]
    [Authorize(Roles = "Admin,Consultant")]
    public async Task<ActionResult<ReservationNoteDto>> UpdateNote(int id, int noteId, [FromBody] UpdateNoteRequest req)
    {
        var note = await db.ReservationNotes.FirstOrDefaultAsync(n => n.Id == noteId && n.ReservationId == id);
        if (note is null) return NotFound();

        var role = User.FindFirstValue(ClaimTypes.Role);
        var (userId, _) = await GetCurrentUserAsync();
        if (role != "Admin" && note.AuthorUserId != userId)
            return Forbid();

        note.Body = req.Body;
        note.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new ReservationNoteDto(note.Id, note.Body, note.AuthorUserId, note.AuthorName, note.CreatedAt, note.UpdatedAt, true));
    }

    // 소프트 삭제(D15) — 상담 기록이 0건일 때만. 미배정이어도 허용(D17, 중복·장난 신청 정리 목적).
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,Consultant")]
    public async Task<IActionResult> SoftDelete(int id)
    {
        var now = DateTimeOffset.UtcNow;
        var (userId, userName) = await GetCurrentUserAsync();

        // 🔴 삭제 UPDATE와 그 처리 이력(reservation_logs) 기록을 하나의 트랜잭션으로 묶는다 — 로그 기록이
        // 실패해도 "삭제됐는데 이력이 없는" 불일치를 막는다(재감사 1번 결함과 동일 패턴).
        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            // 조건(상담기록 0건)과 갱신이 같은 문장에서 평가되므로 경쟁 조건이 없다(11-2절)
            var affected = await db.Reservations
                .Where(r => r.Id == id && !db.ReservationNotes.Any(n => n.ReservationId == id))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.DeletedAt, now)
                    .SetProperty(r => r.DeletedByUserId, userId));

            if (affected == 0)
            {
                var exists = await db.Reservations.AnyAsync(r => r.Id == id);
                if (!exists) return NotFound();
                return Conflict(new { code = "RESERVATION_HAS_NOTES" });
            }

            db.ReservationLogs.Add(new ReservationLog { ReservationId = id, Action = "deleted", ActorUserId = userId, ActorName = userName, CreatedAt = now });
            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }

        // 🔴 audit_logs는 위 트랜잭션과 의도적으로 분리한 베스트에포트 기록이다 — 삭제 자체(위)는 이미
        // 커밋 완료됐으므로, 감사 로그 저장이 실패해도 그 실패가 이미 끝난 삭제를 실패로 보이게 하지
        // 않는다(16장 체크리스트 "감사 로그 저장 실패가 본 작업을 실패시키지 않도록 격리"의 원래 취지 —
        // reservation_logs처럼 삭제 자체와 함께 롤백돼야 하는 것과는 반대로, 이건 원래도 "실패해도 무방한" 부가 기록).
        try
        {
            var actor = await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => new { u.Email, u.Role }).FirstOrDefaultAsync();
            db.AuditLogs.Add(new AuditLog
            {
                ActorUserId = userId,
                ActorEmail = actor?.Email ?? "SYSTEM",
                ActorRole = actor?.Role ?? "",
                Action = "soft_delete",
                EntityType = "reservation",
                EntityId = id.ToString(),
                Summary = $"예약 #{id} 소프트 삭제(상담 기록 0건)",
                StatusCode = 204,
                CreatedAt = now,
            });
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "소프트 삭제 감사 로그 기록 실패: reservationId={ReservationId}", id);
        }

        return NoContent();
    }

    // affected==0의 이유가 셋(없음/미배정/상태변경됨)이라 구분해서 응답해야 화면이 올바른 안내를 띄운다(10-1절)
    private async Task<ActionResult<ReservationDetailDto>> DiagnoseWriteFailureAsync(int id)
    {
        var cur = await db.Reservations.AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new { r.Status, r.ConsultantId })
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

    // 검색어의 %, _, \ 가 그대로 들어가면 LIKE 패턴이 깨지거나 의도치 않은 광범위 매칭이 된다.
    private static string EscapeLike(string s) => s.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
