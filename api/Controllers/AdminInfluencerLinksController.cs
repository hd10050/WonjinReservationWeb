using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using WonjinApi.Data;
using WonjinApi.DTOs;
using WonjinApi.Models;
using WonjinApi.Utils;

namespace WonjinApi.Controllers;

// 인플루언서 짧은 링크 관리(B안, 2026-08-27 신설, 15-2절 연장) — [유입 경로 분석] 화면 안의 관리
// 폼에서만 쓰인다(별도 상위 메뉴 아님). D5와 동일하게 어드민 전용 — 클래스 레벨을 다중 role로
// 열지 않았으므로 6-3절 원칙1(쓰기 액션 재점검)은 해당 없음.
// code는 /go/{code} URL 경로 세그먼트가 되어 이미 배포됐을 수 있으므로 생성 후 변경 불가 —
// Update 요청에 code가 없다. DELETE 없음 — 비활성화는 PUT의 isActive=false로.
[ApiController]
[Route("api/admin/influencer-links")]
[Authorize(Roles = "Admin")]
public class AdminInfluencerLinksController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<InfluencerLinkDto>>> GetList(
        [FromQuery] bool includeInactive = false,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = db.InfluencerLinks.AsQueryable();
        if (!includeInactive)
            query = query.Where(l => l.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = LikeEscape.Escape(search.Trim());
            query = query.Where(l =>
                EF.Functions.ILike(l.Code, $"%{keyword}%", "\\")
                || EF.Functions.ILike(l.DisplayName, $"%{keyword}%", "\\"));
        }

        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(page, 1);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new InfluencerLinkDto(
                l.Id, l.Code, l.DisplayName, l.UtmSource, l.UtmMedium, l.UtmCampaign, l.Locale, l.IsActive, l.CreatedAt))
            .ToListAsync();

        return Ok(new PagedResult<InfluencerLinkDto>(items, total, page, pageSize));
    }

    [HttpPost]
    [EnableRateLimiting("admin-write")]
    public async Task<ActionResult<InfluencerLinkDto>> Create([FromBody] CreateInfluencerLinkRequest req)
    {
        // [Required, MaxLength]는 애노테이션이 이미 걸렀고(ApiController가 자동 400, Program.cs에서
        // {code:"VALIDATION_FAILED"} 형식으로 통일됨), 4개 값 중 하나인지는 별도 화이트리스트 검증 필요
        // (기존 공개 예약 신청과 동일한 에러 코드 재사용 — 새 코드를 만들지 않는다).
        if (req.Locale is not ("zh-CN" or "zh-TW" or "en" or "ko"))
            return BadRequest(new { code = "UNSUPPORTED_LOCALE" });
        if (await db.InfluencerLinks.AnyAsync(l => l.Code == req.Code))
            return BadRequest(new { code = "INFLUENCER_CODE_DUPLICATE" });

        var now = DateTimeOffset.UtcNow;
        var link = new InfluencerLink
        {
            Code = req.Code,
            DisplayName = req.DisplayName,
            UtmSource = req.UtmSource ?? "",
            UtmMedium = string.IsNullOrWhiteSpace(req.UtmMedium) ? "influencer" : req.UtmMedium,
            UtmCampaign = req.UtmCampaign ?? "",
            Locale = req.Locale,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.InfluencerLinks.Add(link);
        await db.SaveChangesAsync();

        return Ok(new InfluencerLinkDto(
            link.Id, link.Code, link.DisplayName, link.UtmSource, link.UtmMedium, link.UtmCampaign, link.Locale, link.IsActive, link.CreatedAt));
    }

    [HttpPut("{id:int}")]
    [EnableRateLimiting("admin-write")]
    public async Task<ActionResult<InfluencerLinkDto>> Update(int id, [FromBody] UpdateInfluencerLinkRequest req)
    {
        if (req.Locale is not ("zh-CN" or "zh-TW" or "en" or "ko"))
            return BadRequest(new { code = "UNSUPPORTED_LOCALE" });

        var link = await db.InfluencerLinks.FirstOrDefaultAsync(l => l.Id == id);
        if (link is null) return NotFound();

        link.DisplayName = req.DisplayName;
        link.UtmSource = req.UtmSource ?? "";
        link.UtmMedium = string.IsNullOrWhiteSpace(req.UtmMedium) ? "influencer" : req.UtmMedium;
        link.UtmCampaign = req.UtmCampaign ?? "";
        link.Locale = req.Locale;
        link.IsActive = req.IsActive;
        link.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new InfluencerLinkDto(
            link.Id, link.Code, link.DisplayName, link.UtmSource, link.UtmMedium, link.UtmCampaign, link.Locale, link.IsActive, link.CreatedAt));
    }
}
