using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WonjinApi.Data;
using WonjinApi.DTOs;
using WonjinApi.Models;

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

    [HttpPost]
    [Authorize(Roles = "Admin,HospitalManager")]
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
}
