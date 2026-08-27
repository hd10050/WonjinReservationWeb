using System.ComponentModel.DataAnnotations;

namespace WonjinApi.DTOs;

// 인플루언서 짧은 링크(/go/{code}) 관리(B안, 2026-08-27 신설, 15-2절 연장) — [유입 경로 분석] 화면
// 안의 관리 폼 전용. record 검증 애노테이션은 파라미터에 직접 부착(11-8절 함정, [property:] 금지).
public record InfluencerLinkDto(
    int Id, string Code, string DisplayName,
    string UtmSource, string UtmMedium, string UtmCampaign, string Locale,
    bool IsActive, DateTimeOffset CreatedAt);

// Code는 /go/{code} URL 경로 세그먼트가 되므로 영문·숫자·하이픈·언더스코어만 허용한다(공유 URL이
// 깨지지 않도록 하는 시스템 경계 검증 — 다른 필드처럼 자유 문자열이 아니다).
public record CreateInfluencerLinkRequest(
    [Required, MaxLength(50), RegularExpression("^[A-Za-z0-9_-]+$")] string Code,
    [Required, MaxLength(100)] string DisplayName,
    [MaxLength(100)] string? UtmSource,
    [MaxLength(100)] string? UtmMedium,
    [MaxLength(100)] string? UtmCampaign,
    [Required, MaxLength(10)] string Locale);

// Code는 없음 — 이미 배포된 공유 URL이 깨지지 않도록 생성 후 변경 불가.
public record UpdateInfluencerLinkRequest(
    [Required, MaxLength(100)] string DisplayName,
    [MaxLength(100)] string? UtmSource,
    [MaxLength(100)] string? UtmMedium,
    [MaxLength(100)] string? UtmCampaign,
    [Required, MaxLength(10)] string Locale,
    bool IsActive);
