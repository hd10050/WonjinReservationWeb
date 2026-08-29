// 인플루언서 짧은 링크(B안, 2026-08-27 신설) — /go/{code}가 백엔드 매핑 테이블에서 UTM·로케일을
// 조회해 실제 랜딩으로 302 리다이렉트한다. 인플루언서는 이 짧은 경로 하나만 기억하면 되고
// UTM 파라미터 이름·값을 몰라도 된다(design.md 15-3절). server/api/[...].ts와 달리 /api/ 하위가
// 아니므로 그 프록시와 겹치지 않는다.
// 🔴 프리픽스는 i18n locales[].code 그대로(/zh-TW) — canonical·hreflang(/zh-TW)과 대소문자를
// 일치시켜 실사용자 착지 URL이 정규 URL과 어긋나지 않게 한다(2026-08-30 SEO 감사 반영).
const LOCALE_PREFIX: Record<string, string> = {
  'zh-CN': '',
  'zh-TW': '/zh-TW',
  en: '/en',
  ko: '/ko',
}

export default defineEventHandler(async (event) => {
  const code = getRouterParam(event, 'code') ?? ''
  const config = useRuntimeConfig()

  try {
    // 🔴 버그(2026-08-28 사용자 지적 — 인플루언서 링크 방문이 통계에 안 잡히고 쿼리스트링 없는
    // 생 URL만 잡힘) — timeout이 2000ms로 너무 짧아, 이 내부 조회 응답이 2초를 넘기면 타임아웃 →
    // catch로 떨어져 아래처럼 완전 무속성 리다이렉트가 나갔다. Cloudflare Workers는 fetch 대기
    // 시간이 CPU 시간에 포함되지 않고 수신 요청의 wall time도 무제한이라(Context7 공식문서 확인)
    // 넉넉히 늘려도 플랫폼 제약에 걸리지 않는다.
    const link = await $fetch<{ utmSource: string, utmMedium: string, utmCampaign: string, locale: string }>(
      `/api/internal/influencer-links/${encodeURIComponent(code)}`,
      {
        baseURL: config.apiBaseInternal as string,
        headers: { 'X-Internal-Secret': config.internalSecret as string },
        timeout: 10000,
      },
    )
    const prefix = LOCALE_PREFIX[link.locale] ?? ''
    const query = new URLSearchParams({
      ref: code,
      utm_source: link.utmSource,
      utm_medium: link.utmMedium,
      utm_campaign: link.utmCampaign,
    })
    return sendRedirect(event, `${prefix}/?${query.toString()}`, 302)
  }
  catch {
    // 존재하지 않거나 비활성 코드 — 원인을 노출하지 않고 조용히 홈으로(11-1절 F11과 동일 원칙,
    // 코드가 유효한지 여부를 응답 형태로 노출하지 않는다는 성질은 유지).
    // 🔴 단, code 자체는 URL에서 이미 알고 있으므로 ref만이라도 실어 보낸다 — UTM 조합(utm_source 등)은
    // DB 조회가 성공해야만 알 수 있어 못 채우지만, 완전 무속성(생 URL)으로 떨어뜨리면 위 타임아웃
    // 케이스에서 그 코드의 방문 자체가 통계에서 통째로 사라진다(사용자가 실사용 중 발견한 버그).
    return sendRedirect(event, `/?${new URLSearchParams({ ref: code }).toString()}`, 302)
  }
})
