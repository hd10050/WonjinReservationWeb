using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WonjinApi.Data;
using WonjinApi.DTOs;
using WonjinApi.Models;
using WonjinApi.Services;

namespace WonjinApi.Controllers;

// [계정 관리] 어드민 전용(6-2절 매트릭스) — 다중 role로 열 필요 없이 컨트롤러 레벨 자체가 방어선.
[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
public class AdminUsersController(AppDbContext db, IPasswordService pwd, IRefreshTokenService rtService) : ControllerBase
{
    private static readonly string[] ValidRoles = ["Admin", "HospitalManager", "Consultant"];

    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminUserDto>>> GetList(
        [FromQuery] string? role,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = db.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(u => u.Role == role); // ix_users_role(8-1절)

        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(page, 1);

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AdminUserDto(u.Id, u.Email, u.Role, u.Name, u.Locale, u.IsSuspended, u.CreatedAt))
            .ToListAsync();

        return Ok(new PagedResult<AdminUserDto>(items, total, page, pageSize));
    }

    [HttpPost]
    public async Task<ActionResult<AdminUserDto>> Create([FromBody] CreateUserRequest req)
    {
        if (!ValidRoles.Contains(req.Role))
            return BadRequest(new { code = "INVALID_ROLE" });

        var email = req.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email))
            return BadRequest(new { code = "EMAIL_ALREADY_EXISTS" });

        var now = DateTimeOffset.UtcNow;
        var user = new User
        {
            Email = email,
            PasswordHash = pwd.Hash(req.Password),
            Role = req.Role,
            Name = req.Name,
            Locale = "ko",
            IsSuspended = false,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        HttpContext.Items["AuditSummary"] = $"계정 #{user.Id}({user.Email}) 발급 — 역할 {user.Role}";
        return Ok(new AdminUserDto(user.Id, user.Email, user.Role, user.Name, user.Locale, user.IsSuspended, user.CreatedAt));
    }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult<AdminUserDto>> Update(int id, [FromBody] UpdateUserRequest req)
    {
        if (req.Role is null && req.IsSuspended is null)
            return BadRequest(new { code = "NO_CHANGES" });
        if (req.Role is not null && !ValidRoles.Contains(req.Role))
            return BadRequest(new { code = "INVALID_ROLE" });

        // 자기 자신의 역할·정지 상태는 조작 불가 — 실수로 자기 권한을 낮추거나 자기 계정을 정지시켜
        // 아무도 못 푸는 상태가 되는 것을 막는다(admin-panel-pattern-reference.md 6절, 자기자신 조작 방지 필수).
        if (id == GetSelfId())
            return BadRequest(new { code = "CANNOT_MODIFY_SELF" });

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();

        var prevRole = user.Role;
        var prevSuspended = user.IsSuspended;
        var changed = false;

        if (req.Role is not null && req.Role != user.Role) { user.Role = req.Role; changed = true; }
        if (req.IsSuspended is not null && req.IsSuspended != user.IsSuspended) { user.IsSuspended = req.IsSuspended.Value; changed = true; }

        if (!changed) return Ok(ToDto(user));

        user.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        // 역할 변경·정지 둘 다 세션을 즉시 무력화한다 — AT는 최대 15분 남아있을 수 있어 RT를 전량
        // 폐기해 다음 자동갱신(12분 간격)에서 막는다(7-3절 AccountStateFilter와 함께 완전 차단).
        await rtService.RevokeAllForUserAsync(user.Id);

        HttpContext.Items["AuditSummary"] =
            $"계정 #{user.Id}({user.Email}) 변경 — 역할 {prevRole}→{user.Role}, 정지 {prevSuspended}→{user.IsSuspended}";

        return Ok(ToDto(user));
    }

    private int? GetSelfId()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return int.TryParse(userIdStr, out var id) ? id : null;
    }

    private static AdminUserDto ToDto(User u) => new(u.Id, u.Email, u.Role, u.Name, u.Locale, u.IsSuspended, u.CreatedAt);
}
