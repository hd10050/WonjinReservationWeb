// 공개 페이지 SEO 메타 일괄 처리(seo-pattern-reference.md 2장 패턴). hreflang(useLocaleHead)과
// JSON-LD(MedicalClinic+Organization)는 layouts/landing.vue가 이미 사이트 전체 공용 정보로
// 전담하므로(D18 브랜드 토큰과 동일 근거 — 페이지마다 다른 조직 정보가 필요 없는 구조), 여기서는
// title/description/OG/canonical/robots만 다룬다.
interface SeoOptions {
  title: MaybeRefOrGetter<string>
  description?: MaybeRefOrGetter<string | undefined>
  noIndex?: MaybeRefOrGetter<boolean>
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
  }))
}
