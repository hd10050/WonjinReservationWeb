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

    // 타이밍 사이드채널 방지 — 시크릿 비교는 항상 상수 시간으로.
    private static bool FixedTimeEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
