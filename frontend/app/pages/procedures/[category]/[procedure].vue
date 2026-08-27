<!-- frontend/app/pages/procedures/[category]/[procedure].vue -->
<template>
  <div v-if="item" class="mx-auto max-w-3xl px-4 py-12">
    <div class="grid gap-8 sm:grid-cols-2 sm:items-center">
      <div>
        <p v-if="item.label?.[locale as Locale]" class="mb-2 text-sm text-muted-foreground">{{ item.label[locale as Locale] }}</p>
        <h1 class="text-3xl font-bold text-foreground">{{ item.name[locale as Locale] }}</h1>
        <p v-if="item.description[locale as Locale]" class="mt-4 whitespace-pre-line text-muted-foreground">{{ item.description[locale as Locale] }}</p>
        <Button as-child class="mt-6">
          <NuxtLink :to="localePath('inquiry')">{{ t('procedures.inquireCta') }}</NuxtLink>
        </Button>
      </div>
      <img
        :src="`/img/${item.imageCategory ?? categorySlug}/${item.image}`"
        :alt="item.name[locale as Locale]"
        class="w-full rounded-xl object-cover"
      >
    </div>
  </div>

  <div v-else-if="other" class="mx-auto max-w-3xl px-4 py-16 text-center">
    <h1 class="text-2xl font-bold text-foreground">{{ other.name[locale as Locale] }}</h1>
    <p class="mt-4 text-muted-foreground">{{ t('procedures.comingSoon') }}</p>
    <Button as-child class="mt-6">
      <NuxtLink :to="localePath('inquiry')">{{ t('procedures.inquireCta') }}</NuxtLink>
    </Button>
  </div>
</template>

<script setup lang="ts">
import { findProcedure, type Locale } from '~/data/procedures'

definePageMeta({ layout: 'landing' })

const route = useRoute()
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
})
</script>
