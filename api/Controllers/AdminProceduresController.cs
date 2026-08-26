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
    // 🔴 보안감사(2026-08-26) 발견 — 페이징이 전혀 없어 테이블이 커지면 매 호출마다 전량 스캔+응답이
    // 된다(DB성능 절대원칙). 이 API는 예약 상세의 시술 다중선택 등에서 "전체 목록"을 배열 그대로
    // 기대하며 재사용 중이라(PagedResult로 바꾸면 호출부가 깨진다), 페이징 UI 대신 안전 상한을 둔다 —
    // 시술은 어드민이 직접 등록하는 마스터 데이터라 500건을 넘을 일이 사실상 없다(20-1절: 시딩 없음).
    [HttpGet]
    public async Task<ActionResult<List<ProcedureLookupDto>>> GetList([FromQuery] bool includeInactive = false)
    {
        var query = db.Procedures.AsQueryable();
        if (!includeInactive)
            query = query.Where(p => p.IsActive);

        var items = await query
            .OrderBy(p => p.SortOrder)
            .Take(500)
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
