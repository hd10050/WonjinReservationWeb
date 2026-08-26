using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WonjinApi.Data;
using WonjinApi.DTOs;
using WonjinApi.Models;

namespace WonjinApi.Controllers;

// 조회(GET)는 Consultant도 필요하다 — 예약 상세의 시술 다중 선택을 채우기 위함(8-3절).
// 등록/수정(POST/PUT)은 11-3절 "HospitalManager 이상"이라 액션 레벨에서 Consultant를 다시 뺀다
// (6-3절 원칙 1). DELETE 없음 — 비활성화는 PUT의 isActive=false로.
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
            .Select(p => new ProcedureLookupDto(p.Id, p.Code, p.NameZhCn, p.NameZhTw, p.NameEn, p.NameKo, p.IsActive, p.SortOrder))
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,HospitalManager")]
    public async Task<ActionResult<ProcedureLookupDto>> Create([FromBody] CreateProcedureRequest req)
    {
        if (await db.Procedures.AnyAsync(p => p.Code == req.Code))
            return BadRequest(new { code = "PROCEDURE_CODE_DUPLICATE" });

        var now = DateTimeOffset.UtcNow;
        var procedure = new Procedure
        {
            Code = req.Code,
            NameZhCn = req.NameZhCn,
            NameZhTw = req.NameZhTw,
            NameEn = req.NameEn,
            NameKo = req.NameKo,
            SortOrder = req.SortOrder,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Procedures.Add(procedure);
        await db.SaveChangesAsync();

        return Ok(new ProcedureLookupDto(procedure.Id, procedure.Code, procedure.NameZhCn, procedure.NameZhTw, procedure.NameEn, procedure.NameKo, procedure.IsActive, procedure.SortOrder));
    }

    // 비활성화도 이 엔드포인트다 — isActive=false로 PUT. code 변경 시에도 UNIQUE 재검증(자기 자신 제외).
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,HospitalManager")]
    public async Task<ActionResult<ProcedureLookupDto>> Update(int id, [FromBody] UpdateProcedureRequest req)
    {
        var procedure = await db.Procedures.FirstOrDefaultAsync(p => p.Id == id);
        if (procedure is null) return NotFound();

        if (await db.Procedures.AnyAsync(p => p.Code == req.Code && p.Id != id))
            return BadRequest(new { code = "PROCEDURE_CODE_DUPLICATE" });

        procedure.Code = req.Code;
        procedure.NameZhCn = req.NameZhCn;
        procedure.NameZhTw = req.NameZhTw;
        procedure.NameEn = req.NameEn;
        procedure.NameKo = req.NameKo;
        procedure.SortOrder = req.SortOrder;
        procedure.IsActive = req.IsActive;
        procedure.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new ProcedureLookupDto(procedure.Id, procedure.Code, procedure.NameZhCn, procedure.NameZhTw, procedure.NameEn, procedure.NameKo, procedure.IsActive, procedure.SortOrder));
    }
}
