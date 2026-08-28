using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc.Filters;
using WonjinApi.Data;
using WonjinApi.Models;

namespace WonjinApi.Filters;

// 전역 감사 로그 자동 기록(14장). Program.cs에서 AccountStateFilter보다 반드시 뒤에 등록할 것 —
// 정지된 요청은 AccountStateFilter가 next()를 호출하지 않고 먼저 401로 끊어버리므로 여기까지 오지 않는다.
public class AuditLogFilter(AppDbContext db, ILogger<AuditLogFilter> logger, IConfiguration config) : IAsyncActionFilter
{
    // 본인 계정 관리 행위(로그인/로그아웃/갱신/내 정보 조회·수정)는 감사 대상이 아니다(14장).
    // /api/auth/me는 GET(Me)뿐 아니라 PATCH(me/password, me/locale)도 이 prefix로 함께 제외되는데,
    // 셋 다 "본인 계정 관리"라 의도된 동작이다.
    // 🔴 보안감사(2026-08-26) 발견 — "/api/admin/audit-logs"는 원래 여기 있었으나 이 컨트롤러는
    // GET만 존재해 애초에 OnActionExecutionAsync 상단(39행)에서 이미 걸러진다. prefix로 남겨두면
    // 나중에 이 경로에 쓰기 액션(로그 삭제 등)이 추가될 때 그 민감한 행위까지 조용히 감사에서
    // 빠지는 회귀 위험이 있어(admin-panel-pattern-reference.md 4-7절), 불필요한 이 항목을 제거했다.
    // /api/admin/push — 본인 기기의 알림 구독 켜기/끄기는 계정 설정 성격이라 /api/auth/me와
    // 동일하게 감사 대상에서 제외한다(2026-08-27, 새 예약 웹 푸시 도입).
    private static readonly string[] ExcludedPrefixes =
    [
        "/api/auth/login", "/api/auth/logout", "/api/auth/refresh", "/api/auth/me", "/api/admin/push",
    ];

    // 14-1절 RouteMap. 세그먼트 개수 내림차순으로 매칭해야 /notes·/status가 상위 규칙에 먹히지 않는다.
    private static readonly (string[] Segments, string Method, string Action, string EntityType)[] RouteMap =
    [
        (["/api/admin/reservations", "/notes"], "POST", "note_add", "reservation_note"),
        (["/api/admin/reservations", "/notes"], "PATCH", "note_update", "reservation_note"),
        (["/api/admin/reservations", "/status"], "POST", "status_change", "reservation"),
        (["/api/admin/reservations"], "PATCH", "update", "reservation"),
        (["/api/admin/reservations", "/consultant"], "PATCH", "assign", "reservation"),
        (["/api/admin/reservations", "/restore"], "POST", "restore", "reservation"),
        (["/api/admin/consultants", "/bulk"], "POST", "bulk_create", "consultant"),
        (["/api/admin/procedures", "/bulk"], "POST", "bulk_create", "procedure"),
        (["/api/admin/categories", "/bulk"], "POST", "bulk_create", "category"),
        (["/api/admin/consultants"], "POST", "create", "consultant"),
        (["/api/admin/consultants"], "PUT", "update", "consultant"),
        (["/api/admin/procedures"], "POST", "create", "procedure"),
        (["/api/admin/procedures"], "PUT", "update", "procedure"),
        (["/api/admin/categories"], "POST", "create", "category"),
        (["/api/admin/categories"], "PUT", "update", "category"),
        (["/api/admin/users"], "POST", "create", "user"),
        (["/api/admin/users"], "PATCH", "update", "user"),
        (["/api/admin/influencer-links"], "POST", "create", "influencer_link"),
        (["/api/admin/influencer-links"], "PUT", "update", "influencer_link"),
    ];

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var method = context.HttpContext.Request.Method.ToUpperInvariant();
        if (method is "GET" or "HEAD" or "OPTIONS") { await next(); return; }

        var path = context.HttpContext.Request.Path.Value?.ToLowerInvariant() ?? "";
        if (ExcludedPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await next();
            return;
        }

        // 감사 대상은 로그인한 3역할 전부(14장의 핵심 차이 — 일반 가이드는 role=="Admin"만 감사하지만
        // 이 프로젝트는 실장·병원관리자 CRUD도 전부 감사해야 한다. Admin으로 좁히면 실장 행위가 통째로 빠진다).
        var role = context.HttpContext.User.FindFirstValue(ClaimTypes.Role);
        if (role is not ("Admin" or "HospitalManager" or "Consultant")) { await next(); return; }

        var executed = await next();

        // 🔴 실측으로 발견: next() 직후엔 액션이 반환한 IActionResult가 아직 "실행"되지 않아
        // HttpContext.Response.StatusCode가 여전히 기본값(200)이다(결과 실행은 액션 필터 파이프라인
        // 바깥, 이후 단계에서 일어남) — BadRequest(400) 등을 반환해도 여기서 곧장 읽으면 200으로
        // 오기록된다(E2E curl로 CANNOT_MODIFY_SELF 400 응답이 감사 로그엔 200으로 남는 것을 확인).
        // IStatusCodeActionResult(Ok/BadRequest/NotFound/Unauthorized 등 표준 결과 전부 구현)에서
        // 의도된 상태코드를 직접 읽는 것이 정확하다. 컨트롤러 예외는 next()가 throw하지 않고
        // Exception에 담아 반환하므로 별도 처리.
        var statusCode = executed.Exception is not null
            ? 500
            : (executed.Result as Microsoft.AspNetCore.Mvc.Infrastructure.IStatusCodeActionResult)?.StatusCode
                ?? context.HttpContext.Response.StatusCode;

        try
        {
            // 🔴 실측으로 발견: 컨트롤러의 SaveChangesAsync가 실패(500)하면 같은 요청 스코프의 DbContext
            // ChangeTracker에 실패한 엔티티가 여전히 Added 상태로 남는다. Clear() 없이 여기서 AuditLog만
            // 추가해 SaveChangesAsync를 다시 호출하면 그 실패한 엔티티까지 함께 재저장을 시도해 동일한
            // DbUpdateException이 또 발생하고, catch에서 삼켜져 **감사 로그 자체가 통째로 유실**된다
            // (20건 동시 요청 중 500이 된 요청은 감사 로그에 단 한 건도 안 남는 것을 실제로 확인).
            // 컨트롤러 성공 여부와 무관하게 감사 로그 저장은 항상 깨끗한 상태에서 시작해야 한다.
            db.ChangeTracker.Clear();

            var candidates = RouteMap.Where(r => r.Method == method && r.Segments.All(s => path.Contains(s)));
            var matched = candidates.OrderByDescending(r => r.Segments.Length).FirstOrDefault();

            var userIdStr = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? context.HttpContext.User.FindFirstValue("sub");
            var actorUserId = int.TryParse(userIdStr, out var uid) ? uid : (int?)null;
            var actorEmail = context.HttpContext.User.FindFirstValue(ClaimTypes.Email) ?? "";

            var entityType = matched.EntityType ?? "unknown";
            var action = matched.Action ?? method.ToLowerInvariant();
            var entityId = context.RouteData.Values.TryGetValue("id", out var idVal) ? idVal?.ToString() : null;

            var summary = context.HttpContext.Items["AuditSummary"] as string
                ?? $"{entityType} {action}" + (entityId is not null ? $" #{entityId}" : "");

            // X-Wj-Client-Ip는 프론트(Workers)를 거친 요청에서만 위조 불가능하다. 백엔드(Render)
            // 직접호출 경로에선 조작 가능하므로 내부시크릿이 유효할 때만 신뢰한다(Program.cs
            // GetClientIp와 동일 원칙 — 이 필터는 별도 클래스라 DI로 재검증).
            // 🔴 2026-08-28 재수정 — 헤더 이름을 CF-Connecting-IP에서 X-Wj-Client-Ip로 변경
            // (Program.cs GetClientIp 주석 참고 — Render도 Cloudflare 엣지 뒤라 그 이름은 재작성됨).
            var providedSecret = context.HttpContext.Request.Headers["X-Internal-Secret"].FirstOrDefault();
            var expectedSecret = config["InternalSecret"];
            var trustProxyIp = !string.IsNullOrEmpty(expectedSecret) && !string.IsNullOrEmpty(providedSecret)
                && CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(providedSecret), Encoding.UTF8.GetBytes(expectedSecret));
            var ip = trustProxyIp
                ? context.HttpContext.Request.Headers["X-Wj-Client-Ip"].FirstOrDefault()
                    ?? context.HttpContext.Connection.RemoteIpAddress?.ToString()
                : context.HttpContext.Connection.RemoteIpAddress?.ToString();

            db.AuditLogs.Add(new AuditLog
            {
                ActorUserId = actorUserId,
                ActorEmail = actorEmail,
                ActorRole = role,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Summary = summary,
                Ip = ip,
                StatusCode = statusCode,
                CreatedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // 감사 로그 저장 실패가 본 작업을 실패시키지 않는다(14장) — try/catch로 격리.
            logger.LogError(ex, "감사 로그 저장 실패");
        }
    }
}
