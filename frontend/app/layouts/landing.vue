<template>
  <div class="flex min-h-screen flex-col bg-background">
    <LandingHeader :overlay="Boolean(route.meta.heroOverlayHeader)" @select-locale="onSelectLocale" />

    <main class="flex-1">
      <slot />
    </main>

    <footer class="border-t bg-card">
      <div class="mx-auto flex max-w-6xl flex-col items-center gap-2 px-4 py-6 text-sm text-muted-foreground">
        <!-- M8 — 사업자정보(상호·사업자번호)는 원문(한국어 등록명) 그대로 표기(고유명사 번역 금지 원칙).
             대표전화는 화면에 노출하지 않고 JSON-LD에만 포함(예약 폼 유도 우선, 2026-08-26 사용자 결정).
             주소만 예외: 2026-08-26 사용자 지시로 로케일별 표기 문구를 분리(ko는 등록원문 유지,
             zh-CN/zh-TW/en은 별도 제공 문구 — design.md D22 참고). -->
        <p class="text-xs">
          {{ t('landing.footer.businessName') }}: {{ BUSINESS_NAME }} · {{ t('landing.footer.businessRegNo') }}: {{ BUSINESS_REG_NO }}
        </p>
        <p class="text-xs">{{ t('landing.footer.address') }}: {{ businessAddress }}</p>
        <div class="mt-2 flex gap-4">
          <NuxtLink :to="localePath('privacy')">{{ t('landing.footer.privacy') }}</NuxtLink>
          <!-- 12-2절 — 저작권 표기 자체가 관리자 로그인 진입점(보안 조치 아님, UI 노출 억제일 뿐) -->
          <NuxtLink to="/admin/login" rel="nofollow">{{ t('landing.footer.copyright', { year: 2026 }) }}</NuxtLink>
        </div>
      </div>
    </footer>

    <InquiryFab v-if="!isInquiryPage" />
  </div>
</template>

<script setup lang="ts">
// 공개 랜딩 전용 레이아웃(12-1절) — index.vue·privacy.vue·procedures/*·inquiry.vue가 공유한다.
// 헤더 마크업은 components/LandingHeader.vue로 추출(로그인 페이지와 공유, 12-2절).
const { t, locale } = useI18n()
const localePath = useLocalePath()
const switchLocalePath = useSwitchLocalePath()

// 헤더의 언어 선택 — 랜딩은 로케일 프리픽스 라우팅이라 해당 언어 경로로 이동 + 수동선택 쿠키 기록.
async function onSelectLocale(code: string) {
  markManualLocale(code)
  await navigateTo(switchLocalePath(code))
}

const route = useRoute()
const isInquiryPage = computed(() => route.path.replace(/\/$/, '') === localePath('inquiry').replace(/\/$/, ''))
const config = useRuntimeConfig()
// SSR h3 이벤트 참조 — 아래 방문기록 fire-and-forget을 event.waitUntil로 묶기 위해 setup 최상단에서
// (Nuxt 인스턴스가 확실히 활성인 지점에서) 미리 잡아둔다. useRequestEvent()를 if 블록 안에서 늦게
// 호출하면 NUXT_E1001(인스턴스 컨텍스트 밖 호출) 경고가 뜬다 — 실측 확인.
const nuxtApp = useNuxtApp()

// 🔴 UTM 캡처 + landing-visit 방문기록은 광고가 어느 페이지로든(시술 상세 딥링크 포함) 착지할 수
// 있으므로 홈 페이지가 아니라 이 레이아웃(모든 공개 페이지 공용)에서 잡는다(최종 리뷰 발견 —
// 이전엔 index.vue에만 있어 딥링크 유입의 UTM·방문집계가 전부 유실됐다. 재검증에서 landing-visit
// 이전 누락이 재지적됨 — /admin/referrals 퍼널 집계가 base set을 landing_daily_stats 방문건
// 기준으로 잡아서, 방문이 안 잡히면 그 캠페인의 예약 자체가 통계에서 통째로 사라진다).
// captureUtm()은 쿼리에 UTM 값이 있을 때만 쓰므로 부작용 없다.
captureUtm()

// 15-1절 — 랜딩 SSR 시점에 프론트 서버가 내부 시크릿 헤더와 함께 방문을 기록한다.
// 🔴 await 하지 않는다(F6) — 방문 집계 실패·지연이 랜딩 렌더 응답 시간에 영향을 주면 안 된다.
// 🔴 2026-08-28 정정 (인플루언서 링크 방문이 통계에 안 잡히는 버그 재조사) — 세션 (60)은 /go/{code}
// 조회 timeout만 10초로 늘렸으나, 정작 landing_daily_stats에 방문을 쓰는 건 이 호출이고 여기는
// timeout:2000 + await 없는 fire-and-forget 그대로였다. 근본 원인은 ②: Cloudflare Workers는
// SSR 응답을 반환하면 아직 진행 중인 fetch를 그 시점에 강제 종료할 수 있다(waitUntil로 등록하지
// 않은 백그라운드 작업은 완료가 보장되지 않는 것이 Workers 규정 동작 — 원점 응답 속도와 무관).
// 로컬(Nitro dev = Node)에선 프로세스가 응답 후에도 살아있어 이 fetch가 항상 완료돼 재현이 안 됐다.
//  ① timeout 2000 → 10000 (세션 60에서 /go 조회에 적용한 것과 동일 — Cloudflare Workers는 fetch
//     대기 시간이 CPU 시간·요청 wall time 제약에 안 걸림, Context7 공식문서 확인).
//  ② nuxtApp.ssrContext.event.waitUntil — 이 fetch가 끝날 때까지 워커 종료를 미룬다
//     (응답 자체는 블로킹하지 않으므로 F6·화면 깜빡임 원칙과 무관).
if (import.meta.server) {
  const utmQuery = {
    referralCode: (route.query.ref as string) || '',
    utmSource: (route.query.utm_source as string) || '',
    utmMedium: (route.query.utm_medium as string) || '',
    utmCampaign: (route.query.utm_campaign as string) || '',
  }
  const visitTask = $fetch(`${config.apiBaseInternal}/api/internal/landing-visit`, {
    method: 'POST',
    headers: { 'X-Internal-Secret': config.internalSecret as string },
    body: utmQuery,
    timeout: 10000,
  }).catch(() => {})
  nuxtApp.ssrContext?.event?.waitUntil?.(visitTask)
}

// 5-1절 hreflang alternate + <html lang> 자동 생성.
const i18nHead = useLocaleHead({ seo: true })
useHead(() => ({
  htmlAttrs: { lang: i18nHead.value.htmlAttrs?.lang },
  link: [...(i18nHead.value.link || [])],
  meta: [...(i18nHead.value.meta || [])],
}))

// M8(2026-08-26 확정) — 사업자등록증 상 등록 정보. 상호·사업자등록번호는 언어와 무관한 사실이라
// 번역하지 않고 4개 로케일 화면 전부에 원문 그대로 표기한다(고유명사 원형 유지 원칙).
const BUSINESS_NAME = '원진성형외과의원'
const BUSINESS_REG_NO = '824-67-00414'
// 🔴 주소만 예외(2026-08-26 사용자 지시, D22) — ko는 등록원문 유지, zh-CN은 제공된 간체 문구,
// zh-TW/en은 영문 주소를 그대로 사용(사용자가 zh-TW도 영문 표기를 명시적으로 선택).
const ADDRESS_BY_LOCALE: Record<string, string> = {
  ko: '서울시 서초구 강남대로 419 파고다타워 12-18층',
  'zh-CN': '首尔市 瑞草区 江南大路419 PAGODA 12-18楼',
  'zh-TW': 'PAGODA tower 17th floor 1306~6 Seocho-dong Seocho-gu, SEOUL',
  en: 'PAGODA tower 17th floor 1306~6 Seocho-dong Seocho-gu, SEOUL',
}
const businessAddress = computed(() => ADDRESS_BY_LOCALE[locale.value] ?? ADDRESS_BY_LOCALE.ko)
// 🔴 대표전화는 화면에 노출하지 않는다(예약 폼 유도 우선, 2026-08-26 사용자 결정) —
// JSON-LD(검색엔진 메타데이터)에만 넣는다. 성형외과 대표번호만 사용(이 시스템은 성형외과 예약 전용).
const BUSINESS_PHONE = '02-3477-3300'

// M8 JSON-LD(5-5절 형식) — MedicalClinic + Organization을 @graph로 묶는다.
// 🔴 innerHTML + <를 <로 이스케이프 — children으로 넣으면 본문이 비고, 미이스케이프는 저장형 XSS가 된다.
const siteUrl = config.public.siteUrl as string
const jsonLd = {
  '@context': 'https://schema.org',
  '@graph': [
    {
      '@type': 'MedicalClinic',
      name: 'WonJin',
      legalName: BUSINESS_NAME,
      url: siteUrl,
      logo: `${siteUrl}/logo.svg`,
      image: `${siteUrl}/logo.svg`,
      telephone: BUSINESS_PHONE,
      taxID: BUSINESS_REG_NO,
      founder: { '@type': 'Person', name: '강문석' },
      // 2026-08-26 사용자 지시로 화면 주소 표기(D22)와 함께 영문 주소 형식으로 갱신.
      address: {
        '@type': 'PostalAddress',
        streetAddress: 'PAGODA Tower 17F, 1306-6 Seocho-dong',
        addressLocality: 'Seocho-gu',
        addressRegion: 'Seoul',
        addressCountry: 'KR',
      },
    },
    { '@type': 'Organization', name: 'WonJin', legalName: BUSINESS_NAME, url: siteUrl, logo: `${siteUrl}/logo.svg` },
  ],
}
useHead({
  script: [{ type: 'application/ld+json', innerHTML: JSON.stringify(jsonLd).replace(/</g, '\\u003c') }],
})

</script>
