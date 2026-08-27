using System.ComponentModel.DataAnnotations;

namespace WonjinApi.DTOs;

public record AdminUserDto(int Id, string Email, string Role, string Name, string Locale, bool IsSuspended, DateTimeOffset CreatedAt);

// 9-1절 3곳 일치: DB email varchar(254)/name varchar(30)(AppDbContext.cs User). 비밀번호는
// ChangePasswordRequest(AuthDto.cs)와 동일 정책(8~64자) — 해시만 저장되므로 DB 컬럼 길이는 해당 없음.
public record CreateUserRequest(
    [Required, MaxLength(254)] string Email,
    [Required, MinLength(8), MaxLength(64)] string Password,
    [Required] string Role,
    [Required, MaxLength(30)] string Name
);

// 역할 변경·정지/해제 + 이름 변경(2026-08-28 추가, AdminUsersController.Update 참고 — 본인 계정은
// Name만 허용, Role·IsSuspended는 자기자신 조작 방지 원칙(admin-panel-pattern-reference.md 6절)대로 차단).
public record UpdateUserRequest(string? Role, bool? IsSuspended, [MaxLength(30)] string? Name);
