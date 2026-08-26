using System.ComponentModel.DataAnnotations;

namespace WonjinApi.DTOs;

// 공개 예약 신청(11-1절). record 검증 애노테이션은 파라미터에 직접 부착할 것 — [property: ...]는
// 런타임 500을 던진다(11-8절 함정, 실측 확인).
public record ReservationCreateRequest(
    [Required, MaxLength(50)] string Name,
    DateOnly BirthDate,
    [Required] string Gender,
    [Required, MaxLength(50)] string WechatId,
    TimeOnly PreferredContactTime,
    [Required] string Locale,
    bool PrivacyConsent,
    string? Honeypot,
    string? UtmSource,
    string? UtmMedium,
    string? UtmCampaign,
    string? ReferralCode
);

public record ReservationCreateResponse(string Code, string WechatId);
