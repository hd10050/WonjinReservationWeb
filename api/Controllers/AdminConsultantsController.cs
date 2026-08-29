using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using WonjinApi.Data;
using WonjinApi.DTOs;
using WonjinApi.Models;
using WonjinApi.Utils;

namespace WonjinApi.Controllers;

// 조회(GET)는 Consultant도 필요하다 — 실장 간 예약 접근이 전면 허용돼(F8) 담당 재배정에 이 목록이
// 필요하기 때문(6-2절 [실장 관리] 메뉴 자체와는 별개 API). 등록/수정(POST/PUT)은 11-3절 "HospitalManager
// 이상"이라 액션 레벨에서 Consultant를 다시 뺀다 — 컨트롤러를 다중 role로 열었으므로 그 안의 쓰기
// 액션마다 재점검할 것(6-3절 원칙 1). DELETE 없음 — 비활성화는 PUT의 isActive=false로(D13).
[ApiController]
[Route("api/admin/consultants")]
[Authorize(Roles = "Admin,HospitalManager,Consultant")]
public class AdminConsultantsController(AppDbContext db) : ControllerBase
{
    // 🔴 2026-08-27 페이징 전면 적용(DB성능 절대원칙) — 예약 배정 드롭다운·시술 다중선택 등 "전체 목록"이
    // 필요한 호출부(index.vue·reservations/[id].vue)는 pageSize=100(다른 목록 API와 동일한 상한, 실장은
    // 단일 병원 인력이라 이 안에서 충분함)을 명시로 넘기고 .items를 읽도록 함께 수정했다 — 그 호출부들이
    // 깨진다는 이유로 페이징 자체를 안 넣던 이전 결정(2026-08-26)을 대체.
    [HttpGet]
    public async Task<ActionResult<PagedResult<ConsultantLookupDto>>> GetList(
        [FromQuery] bool includeInactive = false,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = db.Consultants.AsQueryable();
        if (!includeInactive)
            query = query.Where(c => c.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = LikeEscape.EscapeContains(search);
            query = query.Where(c => EF.Functions.ILike(c.Name, $"%{keyword}%", "\\"));
        }

        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(page, 1);

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(c => c.SortOrder)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ConsultantLookupDto(c.Id, c.Name, c.IsActive, c.SortOrder))
            .ToListAsync();

        return Ok(new PagedResult<ConsultantLookupDto>(items, total, page, pageSize));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,HospitalManager")]
    [EnableRateLimiting("admin-write")]
    public async Task<ActionResult<ConsultantLookupDto>> Create([FromBody] CreateConsultantRequest req)
    {
        var now = DateTimeOffset.UtcNow;
        var consultant = new Consultant { Name = req.Name, SortOrder = req.SortOrder, IsActive = true, CreatedAt = now, UpdatedAt = now };
        db.Consultants.Add(consultant);
        await db.SaveChangesAsync();

        return Ok(new ConsultantLookupDto(consultant.Id, consultant.Name, consultant.IsActive, consultant.SortOrder));
    }

    // 비활성화도 이 엔드포인트다(D13) — isActive=false로 PUT.
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,HospitalManager")]
    [EnableRateLimiting("admin-write")]
    public async Task<ActionResult<ConsultantLookupDto>> Update(int id, [FromBody] UpdateConsultantRequest req)
    {
        var consultant = await db.Consultants.FirstOrDefaultAsync(c => c.Id == id);
        if (consultant is null) return NotFound();

        consultant.Name = req.Name;
        consultant.SortOrder = req.SortOrder;
        consultant.IsActive = req.IsActive;
        consultant.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new ConsultantLookupDto(consultant.Id, consultant.Name, consultant.IsActive, consultant.SortOrder));
    }

    // 엑셀 일괄등록 — excel-bulk-upload-pattern-reference.md 레이어3(all-or-nothing).
    // 문제 행이 하나라도 있으면 아무것도 저장하지 않고 전체 행 오류를 한 번에 반환한다.
    [HttpPost("bulk")]
    [Authorize(Roles = "Admin,HospitalManager")]
    [EnableRateLimiting("admin-write")]
    public async Task<ActionResult> BulkCreate([FromBody] List<BulkConsultantRequest> requests)
    {
        if (requests.Count == 0) return BadRequest(new { code = "BULK_EMPTY" });
        if (requests.Count > 500) return BadRequest(new { code = "BULK_TOO_MANY" });

        var rowErrors = new List<BulkRowError>();
        foreach (var r in requests)
        {
            if (string.IsNullOrWhiteSpace(r.Name))
                rowErrors.Add(new BulkRowError(r.Row, "BULK_FIELD_REQUIRED", "name"));
            else if (r.Name.Trim().Length > 30)
                rowErrors.Add(new BulkRowError(r.Row, "BULK_FIELD_TOO_LONG", "name", r.Name.Trim().Length, 30));
        }
        if (rowErrors.Count > 0)
            return BadRequest(new { code = "BULK_VALIDATION_FAILED", rowErrors });

        var now = DateTimeOffset.UtcNow;
        var consultants = requests.Select(r => new Consultant
        {
            Name = r.Name!.Trim(),
            SortOrder = r.SortOrder,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        }).ToList();
        db.Consultants.AddRange(consultants);
        HttpContext.Items["AuditSummary"] = $"실장 {consultants.Count}건 일괄등록";
        await db.SaveChangesAsync();

        return Ok(new { successCount = consultants.Count });
    }
}
