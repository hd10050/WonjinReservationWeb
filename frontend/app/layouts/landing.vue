<template>
  <div class="flex min-h-screen flex-col bg-background">
    <header class="border-b bg-card">
      <div class="mx-auto flex max-w-3xl items-center justify-between px-4 py-3">
        <NuxtLink :to="localePath('index')" class="flex items-center">
          <img src="/logo.svg" :alt="t('common.appName')" class="h-6 w-auto">
        </NuxtLink>
        <DropdownMenuRoot>
          <DropdownMenuTrigger
            class="flex items-center gap-1 rounded-full border px-3 py-1.5 text-xs font-medium text-muted-foreground transition-colors hover:border-primary hover:text-foreground aria-expanded:border-primary aria-expanded:text-foreground"
          >
            <Globe class="size-3.5" />
            {{ currentLocaleName }}
            <ChevronDown class="size-3.5" />
          </DropdownMenuTrigger>
          <DropdownMenuPortal>
            <DropdownMenuContent :side-offset="8" align="end" class="z-50 min-w-32 rounded-lg border bg-card p-1 text-sm shadow-md">
              <DropdownMenuItem
                v-for="loc in locales"
                :key="loc.code"
                as-child
                class="block w-full cursor-pointer rounded-md px-3 py-1.5 text-foreground outline-none data-[highlighted]:bg-accent data-[highlighted]:text-accent-foreground"
                :class="{ 'font-semibold': loc.code === locale }"
              >
                <NuxtLink :to="switchLocalePath(loc.code)" @click="markManualLocale(loc.code)">{{ loc.name }}</NuxtLink>
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenuPortal>
        </DropdownMenuRoot>
      </div>
    </header>

    <main class="flex-1">
      <slot />
    </main>

    <footer class="border-t bg-card">
      <div class="mx-auto flex max-w-3xl flex-col items-center gap-2 px-4 py-6 text-sm text-muted-foreground">
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
  </div>
</template>

<script setup lang="ts">
import { ChevronDown, Globe } from '@lucide/vue'
import {
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuPortal,
  DropdownMenuRoot,
  DropdownMenuTrigger,
} from 'reka-ui'

// 공개 랜딩 전용 레이아웃(12-1절) — index.vue·privacy.vue가 공유한다.
const { t, locale, locales } = useI18n()
const localePath = useLocalePath()
const switchLocalePath = useSwitchLocalePath()
const currentLocaleName = computed(() => locales.value.find(l => l.code === locale.value)?.name ?? locale.value)

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
const siteUrl = useRuntimeConfig().public.siteUrl as string
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

// 🔴 5-3절 — 헤더의 언어 선택 UI는 <head> 인라인 감지 스크립트와 같은 raw document.cookie
// 포맷으로 두 쿠키를 함께 기록해야 한다. useCookie()는 값을 JSON 인코딩("ko")해 저장하므로
// 그 스크립트의 정규식 파싱(ko)과 어긋난다 — 반드시 raw document.cookie로 직접 쓸 것.
function markManualLocale(code: string) {
  if (import.meta.server) return
  const expires = new Date(Date.now() + 31536000000).toUTCString()
  document.cookie = `wj_lang=${code}; expires=${expires}; path=/; samesite=lax`
  document.cookie = `wj_lang_manual=1; expires=${expires}; path=/; samesite=lax`
}
</script>
