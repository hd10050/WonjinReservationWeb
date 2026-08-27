// 인플루언서 짧은 링크(B안, 2026-08-27 신설) — /go/{code}가 백엔드 매핑 테이블에서 UTM·로케일을
// 조회해 실제 랜딩으로 302 리다이렉트한다. 인플루언서는 이 짧은 경로 하나만 기억하면 되고
// UTM 파라미터 이름·값을 몰라도 된다(design.md 15-3절). server/api/[...].ts와 달리 /api/ 하위가
// 아니므로 그 프록시와 겹치지 않는다.
const LOCALE_PREFIX: Record<string, string> = {
  'zh-CN': '',
  'zh-TW': '/zh-tw',
  en: '/en',
  ko: '/ko',
}

export default defineEventHandler(async (event) => {
  const code = getRouterParam(event, 'code') ?? ''
  const config = useRuntimeConfig()

  try {
    const link = await $fetch<{ utmSource: string, utmMedium: string, utmCampaign: string, locale: string }>(
      `/api/internal/influencer-links/${encodeURIComponent(code)}`,
      {
        baseURL: config.apiBaseInternal as string,
        headers: { 'X-Internal-Secret': config.internalSecret as string },
        timeout: 2000,
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
    // 존재하지 않거나 비활성 코드 — 원인을 노출하지 않고 조용히 홈으로(11-1절 F11과 동일 원칙)
    return sendRedirect(event, '/', 302)
  }
})
