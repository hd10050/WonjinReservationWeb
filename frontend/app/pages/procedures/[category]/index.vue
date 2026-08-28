<!-- frontend/app/pages/procedures/[category]/index.vue -->
<template>
  <div v-if="category">
    <!-- 히어로 풀블리드 확대(2026-08-28, 랜딩 비주얼 리디자인 7절: min-h-80(320px) → min-h-[70vh]).
         above-the-fold라 스크롤 리빌 대상에서 제외(화면 깜빡임 금지 원칙). -->
    <section
      class="relative flex min-h-[70vh] items-end bg-cover bg-center text-background"
      :style="{ backgroundImage: `linear-gradient(to top, rgba(0,0,0,.7), rgba(0,0,0,.25)), url(/img/hero/${category.heroImages[0]})` }"
    >
      <div class="mx-auto w-full max-w-6xl px-4 pb-12 sm:px-6 sm:pb-16">
        <component :is="CATEGORY_ICONS[category.icon]" class="mb-4 size-10" />
        <h1 class="font-display text-4xl font-bold sm:text-6xl">{{ category.name[locale as Locale] }}</h1>
        <p class="mt-4 max-w-xl text-base text-background/90 sm:text-lg">{{ category.intro[locale as Locale] }}</p>
      </div>
    </section>

    <section
      ref="listTarget"
      class="mx-auto max-w-6xl px-4 py-10 transition-all duration-700 sm:px-6 sm:py-16"
      :class="listRevealed ? 'translate-y-0 opacity-100' : 'translate-y-6 opacity-0'"
    >
      <h2 class="mb-6 text-xl font-semibold text-foreground sm:text-2xl">
        {{ t('procedures.concernHeading', { category: category.name[locale as Locale] }) }}
      </h2>

      <ul class="divide-y divide-border">
        <li v-for="(item, i) in category.items" :key="item.slug">
          <NuxtLink
            :to="localePath({ name: 'procedures-category-procedure', params: { category: category.slug, procedure: item.slug } })"
            class="flex flex-col gap-6 py-8 sm:flex-row sm:items-center"
            :class="{ 'sm:flex-row-reverse': i % 2 === 1 }"
          >
            <img
              :src="`/img/${item.imageCategory ?? category.slug}/${item.image}`"
              :alt="item.name[locale as Locale]"
              loading="lazy"
              class="h-64 w-full rounded-xl object-cover sm:w-96 sm:shrink-0"
            >
            <div class="flex flex-1 flex-col gap-2">
              <ul v-if="item.concerns[locale as Locale]?.length" class="space-y-1 text-sm text-muted-foreground">
                <li v-for="(concern, ci) in item.concerns[locale as Locale]" :key="ci">{{ concern }}</li>
              </ul>
              <h3 class="text-xl font-semibold text-foreground sm:text-2xl">{{ item.name[locale as Locale] }}</h3>
            </div>
          </NuxtLink>
        </li>
      </ul>

      <div v-if="category.otherItems.length" class="mt-10 rounded-lg border bg-muted/30 p-5">
        <h3 class="mb-3 text-sm font-semibold text-muted-foreground">{{ t('procedures.otherHeading') }}</h3>
        <div class="flex flex-wrap gap-2">
          <NuxtLink
            v-for="other in category.otherItems"
            :key="other.slug"
            :to="localePath({ name: 'procedures-category-procedure', params: { category: category.slug, procedure: other.slug } })"
            class="rounded-full border px-3 py-1.5 text-sm text-foreground hover:border-primary"
          >
            {{ other.name[locale as Locale] }}
          </NuxtLink>
        </div>
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
import { findCategory, type Locale } from '~/data/procedures'
import { CATEGORY_ICONS } from '~/utils/categoryIcons'

// heroOverlayHeader(2026-08-28) — 이 페이지도 풀블리드 히어로가 있어 홈과 동일하게 오버레이 헤더 적용.
definePageMeta({ layout: 'landing', heroOverlayHeader: true })

const { target: listTarget, revealed: listRevealed } = useScrollReveal()

const route = useRoute()
const { t, locale } = useI18n()
const localePath = useLocalePath()

const category = computed(() => findCategory(route.params.category as string))

if (!category.value) {
  throw createError({ statusCode: 404, statusMessage: 'Category not found' })
}

useSeo({
  title: () => category.value?.name[locale.value as Locale] ?? '',
  description: () => category.value?.intro[locale.value as Locale] ?? '',
})
</script>
