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

// 역할 변경·정지/해제만 다룬다(11-5절 — "계정 발급·역할 변경·정지"). 이름 변경은 이 엔드포인트 범위 밖.
public record UpdateUserRequest(string? Role, bool? IsSuspended);
