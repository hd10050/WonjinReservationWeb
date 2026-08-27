<!-- frontend/app/pages/index.vue -->
<template>
  <div>
    <section class="mx-auto max-w-3xl px-4 py-12 text-center">
      <h1 class="text-3xl font-bold text-foreground">{{ t('landing.home.heroTitle') }}</h1>
      <p class="mt-3 text-muted-foreground">{{ t('landing.home.heroSubtitle') }}</p>
    </section>

    <section class="border-y bg-muted/30 py-10">
      <div class="mx-auto max-w-3xl px-4">
        <h2 class="mb-6 text-center text-lg font-semibold text-foreground">{{ t('landing.home.categoriesHeading') }}</h2>
        <div class="grid grid-cols-3 gap-3 sm:grid-cols-4">
          <NuxtLink
            v-for="category in PROCEDURE_CATEGORIES"
            :key="category.slug"
            :to="localePath({ name: 'procedures-category', params: { category: category.slug } })"
            class="flex flex-col items-center gap-2 rounded-lg border bg-card p-3 text-center transition-colors hover:border-primary"
          >
            <component :is="categoryIcon(category.icon)" class="size-6 text-primary" />
            <span class="text-xs font-medium text-foreground">{{ category.name[locale as Locale] }}</span>
          </NuxtLink>
        </div>
      </div>
    </section>

    <section class="mx-auto max-w-3xl px-4 py-12">
      <h2 class="mb-4 text-xl font-semibold text-foreground">{{ t('landing.home.introHeading') }}</h2>
      <p class="whitespace-pre-line text-muted-foreground">{{ t('landing.home.introBody') }}</p>
    </section>
  </div>
</template>

<script setup lang="ts">
import * as icons from '@lucide/vue'
import { PROCEDURE_CATEGORIES, type Locale } from '~/data/procedures'

definePageMeta({ layout: 'landing' })

const { t, locale } = useI18n()
const localePath = useLocalePath()

useSeo({
  title: () => t('landing.home.heroTitle'),
  description: () => t('landing.home.heroSubtitle'),
})

function categoryIcon(name: string) {
  return (icons as Record<string, unknown>)[name]
}

// UTM 캡처(Task 3) — 이제 광고 랜딩 지점은 홈이므로 여기서 잡아 쿠키에 저장한다.
captureUtm()

// 15-1절 — 랜딩 SSR 시점에 프론트 서버가 내부 시크릿 헤더와 함께 방문을 기록한다.
// 🔴 await 하지 않는다(F6) — 방문 집계 실패·지연이 랜딩 렌더 응답 시간에 영향을 주면 안 된다.
const route = useRoute()
const config = useRuntimeConfig()
if (import.meta.server) {
  const utmQuery = {
    referralCode: (route.query.ref as string) || '',
    utmSource: (route.query.utm_source as string) || '',
    utmMedium: (route.query.utm_medium as string) || '',
    utmCampaign: (route.query.utm_campaign as string) || '',
  }
  $fetch(`${config.apiBaseInternal}/api/internal/landing-visit`, {
    method: 'POST',
    headers: { 'X-Internal-Secret': config.internalSecret as string },
    body: utmQuery,
    timeout: 2000,
  }).catch(() => {})
}
</script>
