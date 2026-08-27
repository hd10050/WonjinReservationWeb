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
            <component :is="CATEGORY_ICONS[category.icon]" class="size-6 text-primary" />
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
import { PROCEDURE_CATEGORIES, type Locale } from '~/data/procedures'
import { CATEGORY_ICONS } from '~/utils/categoryIcons'

definePageMeta({ layout: 'landing' })

const { t, locale } = useI18n()
const localePath = useLocalePath()

useSeo({
  title: () => t('landing.home.heroTitle'),
  description: () => t('landing.home.heroSubtitle'),
})

// 🔴 UTM 캡처 + landing-visit 방문기록은 layouts/landing.vue로 옮겼다(최종 리뷰 발견 +
// 재검증에서 landing-visit 이전 누락 재지적) — 여기 홈에만 있으면 /procedures/eye/glam-eye?
// utm_source=... 같은 시술별 딥링크 광고가 /admin/referrals 방문집계에서 통째로 빠진다.
</script>
