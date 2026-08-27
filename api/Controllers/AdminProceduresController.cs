using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using WonjinApi.Data;
using WonjinApi.DTOs;
using WonjinApi.Models;
using WonjinApi.Utils;

namespace WonjinApi.Controllers;

// 조회(GET)는 Consultant도 필요하다 — 예약 상세의 시술 다중 선택을 채우기 위함(8-3절).
// 등록/수정(POST/PUT)은 11-3절 "HospitalManager 이상"이라 액션 레벨에서 Consultant를 다시 뺀다
// (6-3절 원칙 1). DELETE 없음 — 비활성화는 PUT의 isActive=false로.
[ApiController]
[Route("api/admin/procedures")]
[Authorize(Roles = "Admin,HospitalManager,Consultant")]
public class AdminProceduresController(AppDbContext db) : ControllerBase
{
    // 🔴 2026-08-27 페이징 전면 적용(DB성능 절대원칙) — 예약 상세의 시술 다중선택(reservations/[id].vue)은
    // pageSize=100(다른 목록 API와 동일한 상한)을 명시로 넘기고 .items를 읽도록 함께 수정했다.
    // 🔴 D25(2026-08-28) — 정렬은 sort_order 폐지, 현재 UI 로케일 이름(name_<locale>) 오름차순.
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProcedureLookupDto>>> GetList(
        [FromQuery] bool includeInactive = false,
        [FromQuery] string? search = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] string? locale = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = db.Procedures.AsQueryable();
        if (!includeInactive)
            query = query.Where(p => p.IsActive);
        // 카테고리별 필터(2026-08-28) — ix_procedures_category_id 인덱스 사용(AddCategories 마이그레이션에서 FK 자동 생성).
        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = LikeEscape.Escape(search.Trim());
            query = query.Where(p =>
                EF.Functions.ILike(p.Code, $"%{keyword}%", "\\")
                || EF.Functions.ILike(p.NameZhCn, $"%{keyword}%", "\\")
                || EF.Functions.ILike(p.NameZhTw, $"%{keyword}%", "\\")
                || EF.Functions.ILike(p.NameEn, $"%{keyword}%", "\\")
                || EF.Functions.ILike(p.NameKo, $"%{keyword}%", "\\"));
        }

        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(page, 1);

        // locale은 쿼리 빌드 시점의 상수라 어떤 OrderBy 람다를 쓸지 C#에서 고른다(각 람다는 단순 속성 접근이라 EF 번역 OK).
        var ordered = locale switch
        {
            "zh-TW" => query.OrderBy(p => p.NameZhTw),
            "en" => query.OrderBy(p => p.NameEn),
            "ko" => query.OrderBy(p => p.NameKo),
            _ => query.OrderBy(p => p.NameZhCn),
        };

        var total = await query.CountAsync();
        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProcedureLookupDto(p.Id, p.Code, p.CategoryId, p.NameZhCn, p.NameZhTw, p.NameEn, p.NameKo, p.IsActive))
            .ToListAsync();

        return Ok(new PagedResult<ProcedureLookupDto>(items, total, page, pageSize));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,HospitalManager")]
    [EnableRateLimiting("admin-write")]
    public async Task<ActionResult<ProcedureLookupDto>> Create([FromBody] CreateProcedureRequest req)
    {
        if (await db.Procedures.AnyAsync(p => p.Code == req.Code))
            return BadRequest(new { code = "PROCEDURE_CODE_DUPLICATE" });
        // 🔴 D25 — 소속 카테고리 존재 검증(AssignConsultant의 CONSULTANT_NOT_FOUND와 대칭). 비-nullable int라
        // 누락 시 0으로 바인딩되는데(11-8절), 0인 카테고리는 존재하지 않으므로 이 검사에 함께 걸린다.
        if (!await db.Categories.AnyAsync(c => c.Id == req.CategoryId))
            return BadRequest(new { code = "CATEGORY_NOT_FOUND" });

        var now = DateTimeOffset.UtcNow;
        var procedure = new Procedure
        {
            Code = req.Code,
            CategoryId = req.CategoryId,
            NameZhCn = req.NameZhCn,
            NameZhTw = req.NameZhTw,
            NameEn = req.NameEn,
            NameKo = req.NameKo,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Procedures.Add(procedure);
        await db.SaveChangesAsync();

        return Ok(new ProcedureLookupDto(procedure.Id, procedure.Code, procedure.CategoryId, procedure.NameZhCn, procedure.NameZhTw, procedure.NameEn, procedure.NameKo, procedure.IsActive));
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
        if (!await db.Categories.AnyAsync(c => c.Id == req.CategoryId))
            return BadRequest(new { code = "CATEGORY_NOT_FOUND" });

        procedure.Code = req.Code;
        procedure.CategoryId = req.CategoryId;
        procedure.NameZhCn = req.NameZhCn;
        procedure.NameZhTw = req.NameZhTw;
        procedure.NameEn = req.NameEn;
        procedure.NameKo = req.NameKo;
        procedure.IsActive = req.IsActive;
        procedure.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new ProcedureLookupDto(procedure.Id, procedure.Code, procedure.CategoryId, procedure.NameZhCn, procedure.NameZhTw, procedure.NameEn, procedure.NameKo, procedure.IsActive));
    }

    // 엑셀 일괄등록 — excel-bulk-upload-pattern-reference.md 레이어3(all-or-nothing).
    // 코드 중복은 "엑셀 내부"와 "기존 DB"를 별도 오류로 구분한다(관리자가 취해야 할 조치가 다름).
    // 🔴 D25 — 소속 카테고리는 카테고리 코드로 지정. 존재하지 않는 코드는 행 오류(BULK_CATEGORY_NOT_FOUND).
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
            CheckField(r.Row, r.CategoryCode, "categoryCode", 30);
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

        // 소속 카테고리 코드 → id 배치 해석(조회 1회). 존재하지 않는 코드면 행 오류.
        var catCodes = requests.Where(r => !string.IsNullOrWhiteSpace(r.CategoryCode)).Select(r => r.CategoryCode!.Trim()).Distinct().ToList();
        var categoryIdByCode = await db.Categories.Where(c => catCodes.Contains(c.Code))
            .ToDictionaryAsync(c => c.Code, c => c.Id);
        foreach (var r in requests)
            if (!string.IsNullOrWhiteSpace(r.CategoryCode) && !categoryIdByCode.ContainsKey(r.CategoryCode!.Trim()))
                rowErrors.Add(new BulkRowError(r.Row, "BULK_CATEGORY_NOT_FOUND", "categoryCode"));

        if (rowErrors.Count > 0)
            return BadRequest(new { code = "BULK_VALIDATION_FAILED", rowErrors });

        var now = DateTimeOffset.UtcNow;
        var procedures = requests.Select(r => new Procedure
        {
            Code = r.Code!.Trim(),
            CategoryId = categoryIdByCode[r.CategoryCode!.Trim()],
            NameZhCn = r.NameZhCn!.Trim(),
            NameZhTw = r.NameZhTw!.Trim(),
            NameEn = r.NameEn!.Trim(),
            NameKo = r.NameKo!.Trim(),
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
