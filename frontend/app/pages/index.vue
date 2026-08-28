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

      <!-- 2026-08-28 폴리스(사용자 지시) — 텍스트+CTA를 하단좌측→중앙우측으로 이동, 3단 순차 등장
           애니메이션 추가(heroFadeUp, 0/150/300ms 지연). above-the-fold지만 데이터가 아니라 이미
           SSR로 렌더된 정적 텍스트의 1회성 장식 연출이라 화면 깜빡임 금지 원칙과 무관. -->
      <div class="relative z-10 mx-auto flex h-full max-w-6xl items-center justify-end px-4 sm:px-6">
        <div class="flex max-w-xl flex-col items-end gap-4 text-right">
          <p class="motion-safe:animate-[heroFadeUp_0.7s_ease-out_both] font-display text-6xl font-black leading-none tracking-tight text-white sm:text-8xl">{{ t('common.appName') }}</p>
          <h1 class="motion-safe:animate-[heroFadeUp_0.7s_ease-out_both] motion-safe:[animation-delay:150ms] text-2xl font-bold text-white sm:text-4xl">{{ t('landing.home.heroTitle') }}</h1>
          <Button as-child size="lg" class="motion-safe:animate-[heroFadeUp_0.7s_ease-out_both] motion-safe:[animation-delay:300ms] mt-4 w-fit">
            <NuxtLink :to="localePath('inquiry')">{{ t('procedures.inquireCta') }}</NuxtLink>
          </Button>
        </div>
      </div>
    </section>

    <section ref="categoriesTarget" class="border-y bg-muted/30 py-16 sm:py-24">
      <div class="mx-auto max-w-6xl px-4 sm:px-6">
        <h2 class="mb-10 text-center text-2xl font-bold text-foreground sm:text-4xl">{{ t('landing.home.categoriesHeading') }}</h2>
        <!-- 2026-08-28 폴리스(사용자 지시) — 카드 순차 등장(스타거). 하나의 IntersectionObserver
             결과(categoriesRevealed)를 전 카드가 공유하되, 인덱스별 transition-delay만 다르게 줘서
             왼쪽부터 순서대로 나타나는 것처럼 보이게 한다(카드마다 별도 옵저버 불필요, 최소 구현). -->
        <div class="grid grid-cols-2 gap-3 sm:grid-cols-3 sm:gap-4 lg:grid-cols-4">
          <NuxtLink
            v-for="(category, i) in PROCEDURE_CATEGORIES"
            :key="category.slug"
            :to="localePath({ name: 'procedures-category', params: { category: category.slug } })"
            class="group relative aspect-[4/5] overflow-hidden rounded-xl transition-all duration-500"
            :class="categoriesRevealed ? 'translate-y-0 opacity-100' : 'translate-y-6 opacity-0'"
            :style="{ transitionDelay: categoriesRevealed ? `${i * 60}ms` : '0ms' }"
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
