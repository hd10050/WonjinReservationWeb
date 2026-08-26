<template>
  <div class="flex min-h-screen flex-col bg-background">
    <header class="border-b bg-card">
      <div class="mx-auto flex max-w-3xl items-center justify-between px-4 py-3">
        <NuxtLink :to="localePath('index')" class="text-lg font-semibold text-foreground">
          {{ t('common.appName') }}
        </NuxtLink>
        <nav class="flex gap-3 text-sm">
          <NuxtLink
            v-for="loc in locales"
            :key="loc.code"
            :to="switchLocalePath(loc.code)"
            class="rounded px-2 py-1"
            :class="loc.code === locale ? 'bg-accent text-accent-foreground' : 'text-muted-foreground hover:text-foreground'"
            @click="markManualLocale(loc.code)"
          >
            {{ loc.name }}
          </NuxtLink>
        </nav>
      </div>
    </header>

    <main class="flex-1">
      <slot />
    </main>

    <footer class="border-t bg-card">
      <div class="mx-auto flex max-w-3xl flex-col items-center gap-2 px-4 py-6 text-sm text-muted-foreground">
        <div class="flex gap-4">
          <NuxtLink :to="localePath('privacy')">{{ t('landing.footer.privacy') }}</NuxtLink>
          <!-- 12-2절 — 저작권 표기 자체가 관리자 로그인 진입점(보안 조치 아님, UI 노출 억제일 뿐) -->
          <NuxtLink to="/admin/login" rel="nofollow">{{ t('landing.footer.copyright', { year: 2026 }) }}</NuxtLink>
        </div>
      </div>
    </footer>
  </div>
</template>

<script setup lang="ts">
// 공개 랜딩 전용 레이아웃(12-1절) — index.vue·privacy.vue가 공유한다.
const { t, locale, locales } = useI18n()
const localePath = useLocalePath()
const switchLocalePath = useSwitchLocalePath()

// 5-1절 hreflang alternate + <html lang> 자동 생성.
const i18nHead = useLocaleHead({ seo: true })
useHead(() => ({
  htmlAttrs: { lang: i18nHead.value.htmlAttrs?.lang },
  link: [...(i18nHead.value.link || [])],
  meta: [...(i18nHead.value.meta || [])],
}))

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
