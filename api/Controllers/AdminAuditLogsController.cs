using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WonjinApi.Data;
using WonjinApi.DTOs;
using WonjinApi.Utils;

namespace WonjinApi.Controllers;

// [로그(감사)] 어드민 전용(6-2절 매트릭스). 조회 전용(GET만) — 이 컨트롤러 자체가 AuditLogFilter의
// 제외 경로(/api/admin/audit-logs)라 자기 조회 행위는 로그로 남지 않는다(14장).
[ApiController]
[Route("api/admin/audit-logs")]
[Authorize(Roles = "Admin")]
public class AdminAuditLogsController(AppDbContext db) : ControllerBase
{
    private static readonly TimeZoneInfo Kst = TimeZoneInfo.FindSystemTimeZoneById("Asia/Seoul");

    [HttpGet]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> GetList(
        [FromQuery] int? actorId,
        [FromQuery] string? entityType,
        [FromQuery] string? action,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = db.AuditLogs.AsQueryable();

        if (actorId.HasValue)
            query = query.Where(a => a.ActorUserId == actorId); // ix_audit_logs_actor_user_id_created_at
        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(a => a.EntityType == entityType); // ix_audit_logs_entity_type_created_at
        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action == action);
        if (from.HasValue)
        {
            var fromUtc = TimeZoneInfo.ConvertTimeToUtc(from.Value.ToDateTime(TimeOnly.MinValue), Kst);
            query = query.Where(a => a.CreatedAt >= fromUtc);
        }
        if (to.HasValue)
        {
            // 종료일 다음날 KST 00:00 미만 — 종료일 하루 전체를 포함시키기 위함(AdminReservationsController와 동일 패턴)
            var toExclusiveUtc = TimeZoneInfo.ConvertTimeToUtc(to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), Kst);
            query = query.Where(a => a.CreatedAt < toExclusiveUtc);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = LikeEscape.EscapeContains(search);
            query = query.Where(a =>
                EF.Functions.ILike(a.Summary, $"%{keyword}%", "\\")
                || EF.Functions.ILike(a.ActorEmail, $"%{keyword}%", "\\"));
        }

        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(page, 1);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.CreatedAt) // ix_audit_logs_created_at
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto(
                a.Id, a.ActorUserId, a.ActorEmail, a.ActorRole, a.Action,
                a.EntityType, a.EntityId, a.Summary, a.Ip, a.StatusCode, a.CreatedAt))
            .ToListAsync();

        return Ok(new PagedResult<AuditLogDto>(items, total, page, pageSize));
    }
}
