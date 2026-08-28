using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WonjinApi.Data;
using WonjinApi.DTOs;

namespace WonjinApi.Controllers;

// 프론트 서버(Nitro)만 호출하는 내부 전용 경로(11-1절, F11). 익명 공개 API가 아니다 —
// 시크릿 헤더가 없거나 다르면 404를 반환해 엔드포인트 존재 자체를 숨긴다(401이 아니다).
[ApiController]
[Route("api/internal")]
public class InternalController(AppDbContext db, IConfiguration config) : ControllerBase
{
    private static readonly TimeZoneInfo Kst = TimeZoneInfo.FindSystemTimeZoneById("Asia/Seoul");

    [HttpPost("landing-visit")]
    public async Task<ActionResult> LandingVisit([FromBody] LandingVisitRequest req, [FromHeader(Name = "X-Internal-Secret")] string? secret)
    {
        var expected = config["InternalSecret"];
        if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(secret) || !FixedTimeEquals(secret, expected))
            return NotFound();

        var statDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Kst).DateTime);
        var referralCode = Truncate(req.ReferralCode, 50);
        var utmSource = Truncate(req.UtmSource, 100);
        var utmMedium = Truncate(req.UtmMedium, 100);
        var utmCampaign = Truncate(req.UtmCampaign, 100);

        // 일별 집계 UPSERT(15-1절) — 방문마다 1행이 아니라 (날짜×캠페인조합)당 1행.
        // 키 컬럼이 전부 NOT NULL DEFAULT ''라 NULLS DISTINCT로 인한 무한 중복이 생기지 않는다(8-10절).
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO wonjin.landing_daily_stats
                (stat_date, referral_code, utm_source, utm_medium, utm_campaign, visit_count)
            VALUES ({statDate}, {referralCode}, {utmSource}, {utmMedium}, {utmCampaign}, 1)
            ON CONFLICT (stat_date, referral_code, utm_source, utm_medium, utm_campaign)
            DO UPDATE SET visit_count = wonjin.landing_daily_stats.visit_count + 1
            """);

        return Ok();
    }

    // /go/{code} 리다이렉트 전용(B안, 2026-08-27 신설) — 프론트 서버(Nitro)만 호출한다. landing-visit과
    // 동일한 원칙(11-1절): 시크릿이 없거나 다르면 404로 엔드포인트 존재 자체를 숨긴다. 코드가 없거나
    // 비활성 상태여도 같은 404 — 방문자에게 "이 코드가 존재하는지"를 노출하지 않는다.
    [HttpGet("influencer-links/{code}")]
    public async Task<ActionResult<InfluencerLinkResolveDto>> ResolveInfluencerLink(
        string code, [FromHeader(Name = "X-Internal-Secret")] string? secret)
    {
        var expected = config["InternalSecret"];
        if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(secret) || !FixedTimeEquals(secret, expected))
            return NotFound();

        var link = await db.InfluencerLinks.AsNoTracking()
            .Where(l => l.Code == code && l.IsActive)
            .Select(l => new { l.UtmSource, l.UtmMedium, l.UtmCampaign, l.Locale })
            .FirstOrDefaultAsync();
        if (link is null) return NotFound();

        return Ok(new InfluencerLinkResolveDto(link.UtmSource, link.UtmMedium, link.UtmCampaign, link.Locale));
    }

    // 🔴 임시 진단용(2026-08-28) — IP 레이트리밋이 실배포에서 안 걸리는 원인 확인 후 반드시 삭제할 것.
    // Render가 실제로 받는 헤더값을 그대로 노출해 GetClientIp()의 신뢰 판정이 왜 어긋나는지 확인한다.
    [HttpGet("debug-ip")]
    public ActionResult DebugIp([FromHeader(Name = "X-Internal-Secret")] string? secret)
    {
        var expected = config["InternalSecret"];
        if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(secret) || !FixedTimeEquals(secret, expected))
            return NotFound();

        return Ok(new
        {
            cfConnectingIpHeader = Request.Headers["CF-Connecting-IP"].ToString(),
            xForwardedForHeader = Request.Headers["X-Forwarded-For"].ToString(),
            remoteIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
        });
    }

    // 타이밍 사이드채널 방지 — 시크릿 비교는 항상 상수 시간으로.
    private static bool FixedTimeEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
