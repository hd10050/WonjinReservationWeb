using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WonjinApi.Data;
using WonjinApi.DTOs;

namespace WonjinApi.Controllers;

// Phase 4(시술·수술 관리 CRUD) 이전 최소 조회 전용 — 예약 상세의 시술 다중 선택을 채우기 위함(8-3절).
// 등록/수정/비활성화(PUT)는 Phase 4에서 추가한다.
[ApiController]
[Route("api/admin/procedures")]
[Authorize(Roles = "Admin,HospitalManager,Consultant")]
public class AdminProceduresController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ProcedureLookupDto>>> GetList([FromQuery] bool includeInactive = false)
    {
        var query = db.Procedures.AsQueryable();
        if (!includeInactive)
            query = query.Where(p => p.IsActive);

        var items = await query
            .OrderBy(p => p.SortOrder)
            .Select(p => new ProcedureLookupDto(p.Id, p.NameZhCn, p.NameZhTw, p.NameEn, p.NameKo, p.IsActive, p.SortOrder))
            .ToListAsync();

        return Ok(items);
    }
}
