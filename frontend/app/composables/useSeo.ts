// 공개 페이지 SEO 메타 일괄 처리(seo-pattern-reference.md 2장 패턴). hreflang(useLocaleHead)과
// 사이트 공용 JSON-LD(MedicalClinic+Organization)는 layouts/landing.vue가 전담하므로(D18 브랜드
// 토큰과 동일 근거) 여기서는 title/description/OG/canonical/robots를 다루고, 필요한 페이지만
// schemaOrg로 페이지 단위 구조화 데이터(MedicalProcedure 등)를 추가로 얹는다(5-2절, 2026-08-30 감사 반영).
// 🔴 schemaOrg는 반드시 MaybeRefOrGetter로 받고 useHead(() => ({...})) 게터로 감쌀 것 — 정적 객체를
//    넘기면 최초 호출 시점 값에 고정돼 언어 전환 시 갱신되지 않는다(2장 경고).
interface SeoOptions {
  title: MaybeRefOrGetter<string>
  description?: MaybeRefOrGetter<string | undefined>
  noIndex?: MaybeRefOrGetter<boolean>
  schemaOrg?: MaybeRefOrGetter<Record<string, unknown> | undefined>
}

export function useSeo(options: SeoOptions) {
  const config = useRuntimeConfig()
  const route = useRoute()
  const baseUrl = config.public.siteUrl as string
  const canonicalUrl = computed(() => `${baseUrl}${route.path}`)
  // D18 확정 — 브랜드 토큰은 로케일 무관 고정값(번역 대상 아님). landing.vue JSON-LD와 동일 값.
  const BRAND = 'WonJin'

  const title = () => `${toValue(options.title)} - ${BRAND}`
  const description = () => toValue(options.description)

  useSeoMeta({
    title,
    description,
    robots: () => (toValue(options.noIndex) ? 'noindex, nofollow' : 'index, follow'),
    ogTitle: title,
    ogDescription: description,
    ogImage: `${baseUrl}/og-image.png`,
    ogImageAlt: () => toValue(options.title),
    ogUrl: canonicalUrl,
    ogType: 'website',
    ogSiteName: BRAND,
    twitterCard: 'summary_large_image',
    twitterTitle: title,
    twitterDescription: description,
  })

  useHead(() => ({
    link: [{ rel: 'canonical', href: canonicalUrl.value }],
    // 🔴 innerHTML + '<' → '<' 이스케이프 필수(9장) — children으로 넣으면 본문이 비고,
    //    미이스케이프는 사용자 입력에 '</script>'가 들어올 때 저장형 XSS가 된다. 현재 값은
    //    procedures.ts 정적 상수뿐이지만 landing.vue와 동일한 방어를 그대로 적용한다.
    script: toValue(options.schemaOrg)
      ? [{
          type: 'application/ld+json',
          innerHTML: JSON.stringify(toValue(options.schemaOrg)).replace(/</g, '\\u003c'),
        }]
      : [],
  }))
}
