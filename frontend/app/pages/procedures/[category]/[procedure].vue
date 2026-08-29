<!-- frontend/app/pages/procedures/[category]/[procedure].vue -->
<template>
  <div v-if="item" class="mx-auto max-w-5xl px-4 py-12 sm:px-6 sm:py-16">
    <div class="grid gap-10 sm:grid-cols-2 sm:items-center">
      <div>
        <p v-if="item.label?.[locale as Locale]" class="mb-2 text-sm text-muted-foreground">{{ item.label[locale as Locale] }}</p>
        <h1 class="font-display text-4xl font-bold text-foreground sm:text-5xl">{{ item.name[locale as Locale] }}</h1>
        <p v-if="item.description[locale as Locale]" class="mt-4 whitespace-pre-line text-lg text-muted-foreground">{{ item.description[locale as Locale] }}</p>
        <Button as-child size="lg" class="mt-6">
          <NuxtLink :to="localePath('inquiry')">{{ t('procedures.inquireCta') }}</NuxtLink>
        </Button>
      </div>
      <img
        :src="`/img/${item.imageCategory ?? categorySlug}/${item.image}`"
        :alt="item.name[locale as Locale]"
        class="aspect-square w-full rounded-xl object-cover sm:aspect-[4/5]"
      >
    </div>
  </div>

  <div v-else-if="other" class="mx-auto max-w-5xl px-4 py-16 text-center sm:px-6">
    <h1 class="font-display text-3xl font-bold text-foreground sm:text-4xl">{{ other.name[locale as Locale] }}</h1>
    <p class="mt-4 text-muted-foreground">{{ t('procedures.comingSoon') }}</p>
    <Button as-child size="lg" class="mt-6">
      <NuxtLink :to="localePath('inquiry')">{{ t('procedures.inquireCta') }}</NuxtLink>
    </Button>
  </div>
</template>

<script setup lang="ts">
import { findProcedure, type Locale } from '~/data/procedures'

definePageMeta({ layout: 'landing' })

const route = useRoute()
const config = useRuntimeConfig()
const { t, locale } = useI18n()
const localePath = useLocalePath()

const categorySlug = route.params.category as string
const found = findProcedure(categorySlug, route.params.procedure as string)

if (!found.category || (!found.item && !found.other)) {
  throw createError({ statusCode: 404, statusMessage: 'Procedure not found' })
}

const item = found.item
const other = found.other

useSeo({
  title: () => (item?.name[locale.value as Locale] ?? other?.name[locale.value as Locale] ?? ''),
  description: () => item?.description[locale.value as Locale] ?? '',
  // "그 외"(콘텐츠 없음) 항목은 제목 한 줄뿐이라 검색 색인 대상에서 뺀다(최종 리뷰 발견).
  noIndex: () => !item,
  // 페이지 단위 구조화 데이터(seo-pattern-reference.md 5-2절) — 콘텐츠가 있는 시술만.
  // "그 외" 항목은 noIndex라 스키마도 넣지 않는다.
  schemaOrg: () => item
    ? {
        '@context': 'https://schema.org',
        '@type': 'MedicalProcedure',
        name: item.name[locale.value as Locale],
        description: item.description[locale.value as Locale] || undefined,
        url: `${config.public.siteUrl}${route.path}`,
        provider: { '@type': 'MedicalClinic', name: 'WonJin', url: config.public.siteUrl },
      }
    : undefined,
})
</script>
