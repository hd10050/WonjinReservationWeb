using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
    [EnableRateLimiting("admin-write")]
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
    [EnableRateLimiting("admin-write")]
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

    // 엑셀 일괄등록 — excel-bulk-upload-pattern-reference.md 레이어3(all-or-nothing).
    // 코드 중복은 "엑셀 내부"와 "기존 DB"를 별도 오류로 구분한다(관리자가 취해야 할 조치가 다름).
    [HttpPost("bulk")]
    [Authorize(Roles = "Admin,HospitalManager")]
    [EnableRateLimiting("admin-write")]
    public async Task<ActionResult> BulkCreate([FromBody] List<BulkProcedureRequest> requests)
    {
        if (requests.Count == 0) return BadRequest(new { code = "BULK_EMPTY" });
        if (requests.Count > 500) return BadRequest(new { code = "BULK_TOO_MANY" });

        var rowErrors = new List<BulkRowError>();
        void CheckField(int row, string? value, string field, int max)
        {
            if (string.IsNullOrWhiteSpace(value))
                rowErrors.Add(new BulkRowError(row, "BULK_FIELD_REQUIRED", field));
            else if (value.Trim().Length > max)
                rowErrors.Add(new BulkRowError(row, "BULK_FIELD_TOO_LONG", field, value.Trim().Length, max));
        }
        foreach (var r in requests)
        {
            CheckField(r.Row, r.Code, "code", 30);
            CheckField(r.Row, r.NameZhCn, "nameZhCn", 50);
            CheckField(r.Row, r.NameZhTw, "nameZhTw", 50);
            CheckField(r.Row, r.NameEn, "nameEn", 50);
            CheckField(r.Row, r.NameKo, "nameKo", 50);
        }

        // 엑셀 내부 중복 — 서버 조회 없이 배치 자체에서 계산(코드가 있는 행만 대상).
        var dupInFileRows = requests
            .Where(r => !string.IsNullOrWhiteSpace(r.Code))
            .GroupBy(r => r.Code!.Trim())
            .Where(g => g.Count() > 1)
            .SelectMany(g => g.Select(r => r.Row));
        foreach (var row in dupInFileRows)
            rowErrors.Add(new BulkRowError(row, "BULK_CODE_DUPLICATE_IN_FILE"));

        // 기존 DB와의 중복 — 행 개수만큼 조회하지 않고 배치 조회 1회(DB성능 절대원칙).
        var codes = requests.Where(r => !string.IsNullOrWhiteSpace(r.Code)).Select(r => r.Code!.Trim()).Distinct().ToList();
        var existingCodes = (await db.Procedures.Where(p => codes.Contains(p.Code)).Select(p => p.Code).ToListAsync()).ToHashSet();
        foreach (var r in requests)
            if (!string.IsNullOrWhiteSpace(r.Code) && existingCodes.Contains(r.Code!.Trim()))
                rowErrors.Add(new BulkRowError(r.Row, "BULK_CODE_DUPLICATE_EXISTING"));

        if (rowErrors.Count > 0)
            return BadRequest(new { code = "BULK_VALIDATION_FAILED", rowErrors });

        var now = DateTimeOffset.UtcNow;
        var procedures = requests.Select(r => new Procedure
        {
            Code = r.Code!.Trim(),
            NameZhCn = r.NameZhCn!.Trim(),
            NameZhTw = r.NameZhTw!.Trim(),
            NameEn = r.NameEn!.Trim(),
            NameKo = r.NameKo!.Trim(),
            SortOrder = r.SortOrder,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        }).ToList();
        db.Procedures.AddRange(procedures);
        HttpContext.Items["AuditSummary"] = $"시술 {procedures.Count}건 일괄등록";
        await db.SaveChangesAsync();

        return Ok(new { successCount = procedures.Count });
    }
}
