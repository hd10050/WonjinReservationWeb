using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using WonjinApi.Data;
using WonjinApi.DTOs;
using WonjinApi.Models;
using WonjinApi.Utils;

namespace WonjinApi.Controllers;

// 시술 카테고리 마스터(D25, 8-3-1절). 조회(GET)는 Consultant도 필요하다 — 예약 상세의 시술
// 아코디언 그룹 헤더를 채우기 위함. 등록/수정(POST/PUT)은 11-3절 "HospitalManager 이상"이라
// 액션 레벨에서 Consultant를 다시 뺀다(6-3절 원칙 1). DELETE 없음 — 비활성화는 PUT의 isActive=false로.
[ApiController]
[Route("api/admin/categories")]
[Authorize(Roles = "Admin,HospitalManager,Consultant")]
public class AdminCategoriesController(AppDbContext db) : ControllerBase
{
    // 정렬은 현재 UI 로케일 이름(name_<locale>) 오름차순(D25). "전체 목록"이 필요한 호출부
    // (procedures.vue 카테고리 select·reservations/[id].vue 아코디언)는 pageSize=100을 명시로 넘긴다.
    [HttpGet]
    public async Task<ActionResult<PagedResult<CategoryLookupDto>>> GetList(
        [FromQuery] bool includeInactive = false,
        [FromQuery] string? search = null,
        [FromQuery] string? locale = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = db.Categories.AsQueryable();
        if (!includeInactive)
            query = query.Where(c => c.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = LikeEscape.EscapeContains(search);
            query = query.Where(c =>
                EF.Functions.ILike(c.Code, $"%{keyword}%", "\\")
                || EF.Functions.ILike(c.NameZhCn, $"%{keyword}%", "\\")
                || EF.Functions.ILike(c.NameZhTw, $"%{keyword}%", "\\")
                || EF.Functions.ILike(c.NameEn, $"%{keyword}%", "\\")
                || EF.Functions.ILike(c.NameKo, $"%{keyword}%", "\\"));
        }

        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(page, 1);

        var ordered = locale switch
        {
            "zh-TW" => query.OrderBy(c => c.NameZhTw),
            "en" => query.OrderBy(c => c.NameEn),
            "ko" => query.OrderBy(c => c.NameKo),
            _ => query.OrderBy(c => c.NameZhCn),
        };

        var total = await query.CountAsync();
        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CategoryLookupDto(c.Id, c.Code, c.NameZhCn, c.NameZhTw, c.NameEn, c.NameKo, c.IsActive))
            .ToListAsync();

        return Ok(new PagedResult<CategoryLookupDto>(items, total, page, pageSize));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,HospitalManager")]
    [EnableRateLimiting("admin-write")]
    public async Task<ActionResult<CategoryLookupDto>> Create([FromBody] CreateCategoryRequest req)
    {
        if (await db.Categories.AnyAsync(c => c.Code == req.Code))
            return BadRequest(new { code = "CATEGORY_CODE_DUPLICATE" });

        var now = DateTimeOffset.UtcNow;
        var category = new Category
        {
            Code = req.Code,
            NameZhCn = req.NameZhCn,
            NameZhTw = req.NameZhTw,
            NameEn = req.NameEn,
            NameKo = req.NameKo,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        return Ok(new CategoryLookupDto(category.Id, category.Code, category.NameZhCn, category.NameZhTw, category.NameEn, category.NameKo, category.IsActive));
    }

    // 비활성화도 이 엔드포인트다 — isActive=false로 PUT. code 변경 시에도 UNIQUE 재검증(자기 자신 제외).
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,HospitalManager")]
    [EnableRateLimiting("admin-write")]
    public async Task<ActionResult<CategoryLookupDto>> Update(int id, [FromBody] UpdateCategoryRequest req)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id);
        if (category is null) return NotFound();

        if (await db.Categories.AnyAsync(c => c.Code == req.Code && c.Id != id))
            return BadRequest(new { code = "CATEGORY_CODE_DUPLICATE" });

        category.Code = req.Code;
        category.NameZhCn = req.NameZhCn;
        category.NameZhTw = req.NameZhTw;
        category.NameEn = req.NameEn;
        category.NameKo = req.NameKo;
        category.IsActive = req.IsActive;
        category.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new CategoryLookupDto(category.Id, category.Code, category.NameZhCn, category.NameZhTw, category.NameEn, category.NameKo, category.IsActive));
    }

    // 엑셀 일괄등록 — excel-bulk-upload-pattern-reference.md 레이어3(all-or-nothing).
    // 코드 중복은 "엑셀 내부"와 "기존 DB"를 별도 오류로 구분한다(AdminProceduresController.BulkCreate와 동일).
    [HttpPost("bulk")]
    [Authorize(Roles = "Admin,HospitalManager")]
    [EnableRateLimiting("admin-write")]
    public async Task<ActionResult> BulkCreate([FromBody] List<BulkCategoryRequest> requests)
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

        var dupInFileRows = requests
            .Where(r => !string.IsNullOrWhiteSpace(r.Code))
            .GroupBy(r => r.Code!.Trim())
            .Where(g => g.Count() > 1)
            .SelectMany(g => g.Select(r => r.Row));
        foreach (var row in dupInFileRows)
            rowErrors.Add(new BulkRowError(row, "BULK_CODE_DUPLICATE_IN_FILE"));

        var codes = requests.Where(r => !string.IsNullOrWhiteSpace(r.Code)).Select(r => r.Code!.Trim()).Distinct().ToList();
        var existingCodes = (await db.Categories.Where(c => codes.Contains(c.Code)).Select(c => c.Code).ToListAsync()).ToHashSet();
        foreach (var r in requests)
            if (!string.IsNullOrWhiteSpace(r.Code) && existingCodes.Contains(r.Code!.Trim()))
                rowErrors.Add(new BulkRowError(r.Row, "BULK_CODE_DUPLICATE_EXISTING"));

        if (rowErrors.Count > 0)
            return BadRequest(new { code = "BULK_VALIDATION_FAILED", rowErrors });

        var now = DateTimeOffset.UtcNow;
        var categories = requests.Select(r => new Category
        {
            Code = r.Code!.Trim(),
            NameZhCn = r.NameZhCn!.Trim(),
            NameZhTw = r.NameZhTw!.Trim(),
            NameEn = r.NameEn!.Trim(),
            NameKo = r.NameKo!.Trim(),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        }).ToList();
        db.Categories.AddRange(categories);
        HttpContext.Items["AuditSummary"] = $"카테고리 {categories.Count}건 일괄등록";
        await db.SaveChangesAsync();

        return Ok(new { successCount = categories.Count });
    }
}
