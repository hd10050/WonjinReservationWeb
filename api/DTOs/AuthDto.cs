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

// 🔴 보안감사(2026-08-26) 발견 — CurrentPassword에만 MaxLength가 없어 인증된 요청이 무제한 길이
// 문자열을 검증 없이 BCrypt.Verify로 흘려보낼 수 있었다(입력 필드 길이 제한 절대원칙 위반).
// NewPassword와 동일한 64자로 통일 — 프론트 비밀번호 변경 화면은 아직 없어(N/A) 백엔드만 우선 반영,
// 화면 구현 시 이 값과 동일한 maxlength="64"를 반드시 함께 적용할 것.
public record ChangePasswordRequest(
    [Required, MaxLength(64)] string CurrentPassword,
    [Required, MinLength(8), MaxLength(64)] string NewPassword
);

public record ChangeLocaleRequest([Required] string Locale);
