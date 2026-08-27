using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using WonjinApi.Data;
using WonjinApi.DTOs;
using WonjinApi.Models;
using WonjinApi.Utils;

namespace WonjinApi.Controllers;

// 새 예약 접수 웹 푸시 구독 관리 — 로그인한 어드민(3역할 전부, 예약 대시보드를 보는 모두)만 대상.
// web-push-notification-guide.md의 공개 마케팅 푸시 기본 설계(비로그인 허용, endpoint+auth로만
// 소유권 확인)와 달리, 이 프로젝트는 구독 자체가 [Authorize]라 UserId로 직접 소유권을 확인한다
// (본인 계정 관리 성격이라 AuditLogFilter 제외 목록에도 추가함 — Program.cs 참고).
[ApiController]
[Route("api/admin/push")]
[Authorize]
public class AdminPushController(AppDbContext db, IConfiguration config) : ControllerBase
{
    [HttpGet("public-key")]
    public ActionResult<PushPublicKeyResponse> GetPublicKey()
    {
        var publicKey = config["Push:VapidPublicKey"];
        if (string.IsNullOrEmpty(publicKey)) return NotFound(new { code = "PUSH_NOT_CONFIGURED" });
        return Ok(new PushPublicKeyResponse(publicKey));
    }

    [HttpPost("subscribe")]
    [EnableRateLimiting("admin-write")]
    public async Task<ActionResult> Subscribe([FromBody] PushSubscribeRequest req)
    {
        // 4-1절 — 클라이언트가 보내는 임의 문자열이므로 저장 전 반드시 화이트리스트 검증(SSRF 방지).
        if (!PushEndpointValidator.IsSafe(req.Endpoint))
            return BadRequest(new { code = "UNSUPPORTED_PUSH_ENDPOINT" });

        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var existing = await db.WebPushSubscriptions.FirstOrDefaultAsync(s => s.Endpoint == req.Endpoint);
        if (existing is not null)
        {
            // 같은 기기가 다른 계정으로 재로그인한 경우 등 — 최신 정보로 갱신(가이드 3-5절과 동일 패턴)
            existing.UserId = userId.Value;
            existing.P256dh = req.P256dh;
            existing.Auth = req.Auth;
        }
        else
        {
            db.WebPushSubscriptions.Add(new WebPushSubscription
            {
                Endpoint = req.Endpoint,
                P256dh = req.P256dh,
                Auth = req.Auth,
                UserId = userId.Value,
                CreatedAt = DateTimeOffset.UtcNow,
            });
        }
        await db.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("unsubscribe")]
    [EnableRateLimiting("admin-write")]
    public async Task<ActionResult> Unsubscribe([FromBody] PushUnsubscribeRequest req)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        // 소유권 확인 — endpoint+auth 시크릿 대신(가이드 4-3절 원안) 이미 있는 로그인 정보로
        // "본인 것만" 삭제되게 한다(인증 없는 공개 API가 아니므로 더 간단하고 정확한 방식).
        await db.WebPushSubscriptions
            .Where(s => s.Endpoint == req.Endpoint && s.UserId == userId.Value)
            .ExecuteDeleteAsync();
        return Ok();
    }

    private int? GetUserId()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return int.TryParse(userIdStr, out var id) ? id : null;
    }
}
