using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using WonjinApi.Data;
using WonjinApi.DTOs;
using WonjinApi.Models;
using WonjinApi.Services;

namespace WonjinApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    AppDbContext db,
    IJwtService jwt,
    IPasswordService pwd,
    IRefreshTokenService rtService,
    IConfiguration config,
    IWebHostEnvironment env) : ControllerBase
{
    private const string AtCookie = "wj_at";
    private const string RtCookie = "wj_rt";
    private static readonly string[] SupportedLocales = ["zh-CN", "zh-TW", "en", "ko"];

    // 재감사(2026-08-27, web-security-audit-guide.md 2장 관련) — 존재하지 않는 이메일은 BCrypt
    // 연산 자체를 건너뛰어 존재하는 이메일(느린 BCrypt 검증 수행)과 응답 시간이 달라, 그 차이로
    // 이메일 존재 여부를 추정할 수 있는 타이밍 사이드채널이었다. 실제 비밀번호와는 무관한
    // 더미 해시로 항상 동일한 연산을 수행해 시간을 맞춘다.
    private const string DummyPasswordHash = "$2a$12$k5.U4Dck4.OXc.piWc.QiuSuBW4kxka/CSO7qb51c1ljMZgCsWdi.";

    // 회원가입 엔드포인트는 존재하지 않는다(D6) — 계정 생성은 POST /api/admin/users(Phase 7) 하나뿐.

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<UserDto>> Login([FromBody] LoginRequest req)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);

        var passwordOk = pwd.Verify(req.Password, user?.PasswordHash ?? DummyPasswordHash);
        if (user is null || !passwordOk)
            return Unauthorized(new { code = "INVALID_CREDENTIALS" });

        if (user.IsSuspended)
            return Unauthorized(new { code = "ACCOUNT_SUSPENDED" });

        await SetAuthCookiesAsync(user);
        return Ok(ToDto(user));
    }

    [HttpPost("refresh")]
    [EnableRateLimiting("refresh")]
    public async Task<ActionResult> Refresh()
    {
        var rawToken = Request.Cookies[RtCookie];
        if (string.IsNullOrEmpty(rawToken))
            return Unauthorized(new { code = "NO_REFRESH_TOKEN" });

        var rt = await rtService.ValidateAsync(rawToken);
        if (rt is null || rt.User is null)
            return Unauthorized(new { code = "INVALID_REFRESH_TOKEN" });

        // 정지된 계정은 갱신 차단 + 쿠키 즉시 삭제(7-2절)
        if (rt.User.IsSuspended)
        {
            await rtService.RevokeAsync(rawToken);
            DeleteCookie(AtCookie);
            DeleteCookie(RtCookie);
            return Unauthorized(new { code = "ACCOUNT_SUSPENDED" });
        }

        // RT Rotation — 기존 폐기 후 신규 발급(7-1절)
        await rtService.RevokeAsync(rawToken);
        var newAt = jwt.GenerateAccessToken(rt.User);
        var (newRt, newRawToken) = await rtService.CreateAsync(rt.User.Id);

        SetCookie(AtCookie, newAt, DateTimeOffset.UtcNow.AddMinutes(GetAccessTokenMinutes()));
        SetCookie(RtCookie, newRawToken, newRt.ExpiresAt);

        return Ok();
    }

    [HttpPost("logout")]
    public async Task<ActionResult> Logout()
    {
        var rawToken = Request.Cookies[RtCookie];
        if (!string.IsNullOrEmpty(rawToken))
            await rtService.RevokeAsync(rawToken);

        DeleteCookie(AtCookie);
        DeleteCookie(RtCookie);
        return Ok();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me()
    {
        // 정지·강등 여부는 AccountStateFilter가 이 액션 진입 전에 이미 검사했다([Authorize] 대상, 7-3절).
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var user = await db.Users.FindAsync(userId.Value);
        if (user is null) return Unauthorized();

        return Ok(ToDto(user));
    }

    [HttpPatch("me/password")]
    [Authorize]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var user = await db.Users.FindAsync(userId.Value);
        if (user is null) return Unauthorized();

        if (!pwd.Verify(req.CurrentPassword, user.PasswordHash))
            return BadRequest(new { code = "INVALID_CURRENT_PASSWORD" });

        user.PasswordHash = pwd.Hash(req.NewPassword);
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        // 다른 기기 세션 전부 무효화 + 현재 세션만 재발급(7-2절)
        await rtService.RevokeAllForUserAsync(user.Id);
        await SetAuthCookiesAsync(user);

        return Ok();
    }

    [HttpPatch("me/locale")]
    [Authorize]
    public async Task<ActionResult<UserDto>> ChangeLocale([FromBody] ChangeLocaleRequest req)
    {
        if (!SupportedLocales.Contains(req.Locale))
            return BadRequest(new { code = "UNSUPPORTED_LOCALE" });

        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var user = await db.Users.FindAsync(userId.Value);
        if (user is null) return Unauthorized();

        user.Locale = req.Locale;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        return Ok(ToDto(user));
    }

    private int? GetUserId()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return int.TryParse(userIdStr, out var id) ? id : null;
    }

    private async Task SetAuthCookiesAsync(User user)
    {
        var at = jwt.GenerateAccessToken(user);
        var (rt, rawToken) = await rtService.CreateAsync(user.Id);

        SetCookie(AtCookie, at, DateTimeOffset.UtcNow.AddMinutes(GetAccessTokenMinutes()));
        SetCookie(RtCookie, rawToken, rt.ExpiresAt);
    }

    private int GetAccessTokenMinutes() =>
        int.TryParse(config["Jwt:AccessTokenMinutes"], out var m) ? m : 15;

    private void SetCookie(string name, string value, DateTimeOffset expires)
    {
        Response.Cookies.Append(name, value, new CookieOptions
        {
            HttpOnly = true,
            // 로컬 개발은 http라 Secure 쿠키가 브라우저에 저장되지 않는다 — 배포(Cloudflare, https)만 true.
            Secure = !env.IsDevelopment(),
            SameSite = SameSiteMode.Lax, // 동일 출처 프록시(D7)라 None이 필요 없다(7-1절)
            Expires = expires,
            Path = "/",
        });
    }

    private void DeleteCookie(string name)
    {
        Response.Cookies.Delete(name, new CookieOptions
        {
            Path = "/",
            Secure = !env.IsDevelopment(),
            SameSite = SameSiteMode.Lax,
        });
    }

    private static UserDto ToDto(User user) => new(user.Id, user.Email, user.Role, user.Name, user.Locale);
}
