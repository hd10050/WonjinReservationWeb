using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using WonjinApi.Data;

namespace WonjinApi.Filters;

// 정지·강등을 매 요청마다 즉시 반영한다(7-3절). Me()/Refresh()에만 체크를 두면 다른 모든 API가
// 이미 발급된 AT가 만료될 때까지(최대 15분) 계속 통과한다.
public class AccountStateFilter(AppDbContext db) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // [Authorize]를 요구하지 않는 액션은 통과시킨다. JWT Bearer는 [Authorize] 여부와 무관하게
        // 유효한 AT 쿠키만 있으면 User를 채우므로, 이 확인이 없으면 정지된 유저가 공개 API에서도
        // 401을 맞아 익명 방문자보다 못한 상태가 된다.
        var metadata = context.ActionDescriptor.EndpointMetadata;
        var requiresAuth = metadata.OfType<IAuthorizeData>().Any() && !metadata.OfType<IAllowAnonymous>().Any();
        if (!requiresAuth) { await next(); return; }

        var principal = context.HttpContext.User;
        if (principal.Identity?.IsAuthenticated != true) { await next(); return; }

        // "sub" 단독 조회 금지 — MapInboundClaims 기본값(true)이 "sub"를 NameIdentifier로 재매핑하므로
        // NameIdentifier를 먼저 찾고 "sub"로 폴백하는 순서를 지킨다.
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        if (!int.TryParse(userIdStr, out var userId)) { await next(); return; }

        var current = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.IsSuspended, u.Role })
            .FirstOrDefaultAsync();

        if (current is null || current.IsSuspended)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // 강등·승격 즉시 반영 — 토큰의 Role과 DB의 Role이 다르면 401 → 프론트가 refresh로 새 Role을 받는다
        if (current.Role != principal.FindFirstValue(ClaimTypes.Role))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        await next();
    }
}
