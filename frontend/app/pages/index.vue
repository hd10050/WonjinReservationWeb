<!-- frontend/app/pages/index.vue -->
<template>
  <div>
    <!-- 히어로(2026-08-28, 랜딩 비주얼 리디자인 6절) — 풀블리드 4장 크로스페이드 + Ken Burns 줌.
         above-the-fold라 애니메이션 등장 효과 없이 항상 즉시 보이는 상태(화면 깜빡임 금지 원칙,
         스크롤 리빌은 fold 아래 섹션에만 적용). stemcell-hero.png(1.45MB)는 무거워 후보에서 제외. -->
    <section class="relative min-h-[88vh] w-full overflow-hidden">
      <div
        v-for="(slide, i) in HERO_SLIDES"
        :key="slide"
        class="absolute inset-0 bg-cover bg-center transition-opacity duration-[1500ms] motion-safe:animate-[kenburns_20s_ease-in-out_infinite]"
        :class="i === activeSlide ? 'opacity-100' : 'opacity-0'"
        :style="{ backgroundImage: `url(/img/hero/${slide})` }"
      />
      <div class="absolute inset-0 bg-gradient-to-t from-black/70 via-black/20 to-black/10" />

      <div class="relative z-10 mx-auto flex h-full max-w-6xl flex-col justify-end gap-4 px-4 pb-16 sm:px-6 sm:pb-24">
        <p class="font-display text-6xl font-black leading-none tracking-tight text-white sm:text-8xl">{{ t('common.appName') }}</p>
        <h1 class="max-w-xl text-2xl font-bold text-white sm:text-4xl">{{ t('landing.home.heroTitle') }}</h1>
        <Button as-child size="lg" class="mt-4 w-fit">
          <NuxtLink :to="localePath('inquiry')">{{ t('procedures.inquireCta') }}</NuxtLink>
        </Button>
      </div>
    </section>

    <section
      ref="categoriesTarget"
      class="border-y bg-muted/30 py-16 transition-all duration-700 sm:py-24"
      :class="categoriesRevealed ? 'translate-y-0 opacity-100' : 'translate-y-6 opacity-0'"
    >
      <div class="mx-auto max-w-6xl px-4 sm:px-6">
        <h2 class="mb-10 text-center text-2xl font-bold text-foreground sm:text-4xl">{{ t('landing.home.categoriesHeading') }}</h2>
        <div class="grid grid-cols-2 gap-3 sm:grid-cols-3 sm:gap-4 lg:grid-cols-4">
          <NuxtLink
            v-for="category in PROCEDURE_CATEGORIES"
            :key="category.slug"
            :to="localePath({ name: 'procedures-category', params: { category: category.slug } })"
            class="group relative aspect-[4/5] overflow-hidden rounded-xl"
          >
            <img
              :src="`/img/hero/${category.heroImages[0]}`"
              :alt="category.name[locale as Locale]"
              loading="lazy"
              class="absolute inset-0 size-full object-cover transition-transform duration-500 group-hover:scale-110"
            >
            <div class="absolute inset-0 bg-gradient-to-t from-black/70 via-black/10 to-transparent" />
            <span class="absolute inset-x-0 bottom-0 p-3 text-sm font-semibold text-white sm:text-base">{{ category.name[locale as Locale] }}</span>
          </NuxtLink>
        </div>
      </div>
    </section>

    <section
      ref="introTarget"
      class="mx-auto max-w-4xl px-4 py-16 text-center transition-all duration-700 sm:px-6 sm:py-24"
      :class="introRevealed ? 'translate-y-0 opacity-100' : 'translate-y-6 opacity-0'"
    >
      <h2 class="mb-4 font-display text-3xl font-bold text-foreground sm:text-5xl">{{ t('landing.home.introHeading') }}</h2>
      <p class="whitespace-pre-line text-lg text-muted-foreground">{{ t('landing.home.introBody') }}</p>
    </section>

    <section class="bg-primary px-4 py-16 text-center sm:px-6 sm:py-20">
      <Button as-child size="lg" variant="secondary">
        <NuxtLink :to="localePath('inquiry')">{{ t('procedures.inquireCta') }}</NuxtLink>
      </Button>
    </section>
  </div>
</template>

<script setup lang="ts">
import { PROCEDURE_CATEGORIES, type Locale } from '~/data/procedures'

// 🔴 heroOverlayHeader(2026-08-28, 랜딩 비주얼 리디자인 5절) — layouts/landing.vue가 이 메타를 읽어
// LandingHeader에 overlay prop을 전달한다. 풀블리드 히어로가 있는 페이지만 true로 선언할 것.
definePageMeta({ layout: 'landing', heroOverlayHeader: true })

const { t, locale } = useI18n()
const localePath = useLocalePath()

useSeo({
  title: () => t('landing.home.heroTitle'),
})

// 히어로 크로스페이드(8절) — 순수 CSS(motion-safe:animate-kenburns) + setInterval만 사용, 라이브러리 없음.
// stemcell-hero.png(1.45MB PNG)는 무거워 제외 — eye/nose/contour/lifting 4개 JPG 히어로만 순환.
const HERO_SLIDES = ['eye-hero.jpg', 'nose-hero.jpg', 'contour-hero.jpg', 'lifting-hero.jpg']
const activeSlide = ref(0)
let sliderTimer: ReturnType<typeof setInterval> | undefined

onMounted(() => {
  if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) return
  sliderTimer = setInterval(() => {
    activeSlide.value = (activeSlide.value + 1) % HERO_SLIDES.length
  }, 5000)
})
onUnmounted(() => {
  if (sliderTimer) clearInterval(sliderTimer)
})

// 스크롤 리빌(8절) — fold 아래 두 섹션만 대상. 히어로는 above-the-fold라 대상에서 제외(화면 깜빡임 금지).
const { target: categoriesTarget, revealed: categoriesRevealed } = useScrollReveal()
const { target: introTarget, revealed: introRevealed } = useScrollReveal()

// 🔴 UTM 캡처 + landing-visit 방문기록은 layouts/landing.vue로 옮겼다(최종 리뷰 발견 +
// 재검증에서 landing-visit 이전 누락 재지적) — 여기 홈에만 있으면 /procedures/eye/glam-eye?
// utm_source=... 같은 시술별 딥링크 광고가 /admin/referrals 방문집계에서 통째로 빠진다.
</script>
