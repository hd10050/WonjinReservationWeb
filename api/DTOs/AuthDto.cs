using System.ComponentModel.DataAnnotations;

namespace WonjinApi.DTOs;

// 🔴 record의 검증 애노테이션은 파라미터에 직접 붙일 것 — [property: ...] 타겟 지정자를 쓰면
// ASP.NET Core가 "파라미터와 연결 안 된 속성 메타데이터"로 오인해 500(InvalidOperationException)을
// 던진다(실측 확인). 공식 문서(model-binding.md)도 파라미터 직접 부착만 예시로 제시한다.
public record LoginRequest(
    [Required, MaxLength(254)] string Email,
    [Required, MaxLength(64)] string Password
);

public record UserDto(int Id, string Email, string Role, string Name, string Locale);

public record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required, MinLength(8), MaxLength(64)] string NewPassword
);

public record ChangeLocaleRequest([Required] string Locale);
