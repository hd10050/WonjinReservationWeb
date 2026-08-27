namespace WonjinApi.DTOs;

// 내부 전용(11-1절) — 프론트 서버(Nitro)만 X-Internal-Secret 헤더로 호출한다.
// 익명 공개 API가 아니므로 검증 애노테이션([Required] 등)을 두지 않는다 — 형식이 어긋나도
// 방문 집계 실패가 랜딩 렌더에 영향을 주면 안 된다(F6, fire-and-forget 호출 쪽 원칙과 대칭).
public record LandingVisitRequest(
    string? ReferralCode,
    string? UtmSource,
    string? UtmMedium,
    string? UtmCampaign
);

// /go/{code} 리다이렉트 전용(B안, 2026-08-27 신설) — 프론트 서버(Nitro)만 X-Internal-Secret 헤더로 조회한다.
public record InfluencerLinkResolveDto(string UtmSource, string UtmMedium, string UtmCampaign, string Locale);
