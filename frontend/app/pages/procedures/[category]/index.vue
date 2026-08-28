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

    <!-- 병원 시스템·특장점 섹션(2026-08-28) — 데이터가 있는 카테고리만, "고민이 있으신가요?" 위에. -->
    <ProcedureMedicalSystem v-if="medical" :blocks="medical" />

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
            <!-- 🔴 정렬 일관성 정정(2026-08-28 사용자 재지시 — 가운데 정렬이나 "이미지 옆에 붙임"이
                 아니라, 이미지 반대편 "바깥쪽 끝"에 텍스트가 오도록 통일하라는 의미였음). 이미지가
                 오른쪽(홀수, flex-row-reverse)일 때 텍스트가 왼쪽 끝에 오는 기존 상태가 이미 의도한
                 모습 — 그대로 둔다(기본 좌측 정렬). 이미지가 왼쪽(짝수)일 때만 텍스트를 오른쪽 끝으로
                 밀어 반대편과 대칭이 되게 한다. 즉 이전 수정(홀수에 우측정렬)은 조건이 반대였다. -->
            <div class="flex flex-1 flex-col gap-2" :class="{ 'sm:items-end sm:text-right': i % 2 === 0 }">
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
import { PROCEDURE_MEDICAL } from '~/data/procedureMedical'
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

const medical = computed(() => (category.value ? PROCEDURE_MEDICAL[category.value.slug] : undefined))

useSeo({
  title: () => category.value?.name[locale.value as Locale] ?? '',
  description: () => category.value?.intro[locale.value as Locale] ?? '',
})
</script>
