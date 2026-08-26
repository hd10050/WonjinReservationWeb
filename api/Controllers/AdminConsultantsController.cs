using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WonjinApi.Data;
using WonjinApi.DTOs;

namespace WonjinApi.Controllers;

// Phase 4(실장 관리 CRUD) 이전 최소 조회 전용 — 예약 상세의 담당 실장 배정 드롭다운을 채우기 위함(D8).
// 등록/수정/비활성화(PUT)는 Phase 4에서 추가한다. 조회는 Consultant도 필요하다 — 실장 간 예약 접근이
// 전면 허용돼(F8) 담당 재배정에 이 목록이 필요하기 때문(6-2절 [실장 관리] 메뉴 자체와는 별개 API).
[ApiController]
[Route("api/admin/consultants")]
[Authorize(Roles = "Admin,HospitalManager,Consultant")]
public class AdminConsultantsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ConsultantLookupDto>>> GetList([FromQuery] bool includeInactive = false)
    {
        var query = db.Consultants.AsQueryable();
        if (!includeInactive)
            query = query.Where(c => c.IsActive);

        var items = await query
            .OrderBy(c => c.SortOrder)
            .Select(c => new ConsultantLookupDto(c.Id, c.Name, c.IsActive, c.SortOrder))
            .ToListAsync();

        return Ok(items);
    }
}
