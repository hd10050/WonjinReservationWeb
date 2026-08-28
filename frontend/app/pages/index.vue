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
           애니메이션 추가(heroFadeUp). above-the-fold지만 데이터가 아니라 이미 SSR로 렌더된 정적
           텍스트의 1회성 장식 연출이라 화면 깜빡임 금지 원칙과 무관.
           🔴 정정 — `h-full`(height:100%)은 부모(`section`)가 `min-h-[88vh]`(min-height)만 갖고
           명시적 `height`가 없어 퍼센트 높이 해석 기준이 안 됨(CSS 스펙상 percentage height는
           부모의 "명시된 height"만 인정, min-height는 불인정) — 실측 결과 텍스트 박스가 224px
           높이로 쪼그라들어 맨 위에 붙어있었다(수직 중앙 정렬 실패, 사용자 재지적으로 발견).
           `absolute inset-0`은 containing block의 실제 렌더 박스에 직접 고정되므로 이 문제가 없다.
           🔴 지연값 2배 확대(사용자 재지시, "효과가 잘 안보임") — 0/150/300ms → 0/300/600ms. -->
      <div class="absolute inset-0 z-10 mx-auto flex max-w-6xl items-center justify-end px-4 sm:px-6">
        <div class="flex max-w-xl flex-col items-end gap-4 text-right">
          <p class="motion-safe:animate-[heroFadeUp_0.7s_ease-out_both] font-display text-6xl font-black leading-none tracking-tight text-white sm:text-8xl">{{ t('common.appName') }}</p>
          <h1 class="motion-safe:animate-[heroFadeUp_0.7s_ease-out_both] motion-safe:[animation-delay:300ms] text-2xl font-bold text-white sm:text-4xl">{{ t('landing.home.heroTitle') }}</h1>
          <Button as-child size="lg" class="motion-safe:animate-[heroFadeUp_0.7s_ease-out_both] motion-safe:[animation-delay:600ms] mt-4 w-fit">
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
             왼쪽부터 순서대로 나타나는 것처럼 보이게 한다(카드마다 별도 옵저버 불필요, 최소 구현).
             🔴 카드당 지연 2배 확대(사용자 재지시) — 60ms → 120ms. -->
        <div class="grid grid-cols-2 gap-3 sm:grid-cols-3 sm:gap-4 lg:grid-cols-4">
          <NuxtLink
            v-for="(category, i) in PROCEDURE_CATEGORIES"
            :key="category.slug"
            :to="localePath({ name: 'procedures-category', params: { category: category.slug } })"
            class="group relative aspect-[4/5] overflow-hidden rounded-xl transition-all duration-500"
            :class="categoriesRevealed ? 'translate-y-0 opacity-100' : 'translate-y-6 opacity-0'"
            :style="{ transitionDelay: categoriesRevealed ? `${i * 120}ms` : '0ms' }"
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

    <!-- WJ 원진 소개(2026-08-28, 사용자 지시) — k-wonjin.co.kr/hospitalinfo/about 참고 재구성.
         문구는 그 페이지 원문(ko) + 자매 사이트 wonjincn.com/ycjs/yyjs/(zh-CN 공식 번역, zh-TW는
         간체→번체 기계적 변환) 그대로 사용, en만 동일 의미로 직접 번역(원문 사이트에 대응 문단이
         없었음). 사진 3장은 같은 페이지에서 가져온 실제 병원 인테리어(원진 공식 사이트 소유,
         이 프로젝트도 같은 병원 예약 시스템이라 재사용 무방 — 사용자 명시 승인). -->
    <section
      ref="introTarget"
      class="mx-auto max-w-6xl px-4 py-16 transition-all duration-700 sm:px-6 sm:py-24"
      :class="introRevealed ? 'translate-y-0 opacity-100' : 'translate-y-6 opacity-0'"
    >
      <div class="grid gap-10 sm:grid-cols-2 sm:items-center sm:gap-16">
        <div>
          <p class="mb-3 text-sm font-semibold tracking-widest text-primary uppercase">{{ t('landing.home.introHeading') }}</p>
          <h2 class="mb-6 font-display text-3xl font-bold text-foreground sm:text-5xl">{{ t('landing.home.introTagline') }}</h2>
          <p class="whitespace-pre-line text-lg text-muted-foreground">{{ t('landing.home.introBody') }}</p>
        </div>
        <div class="grid grid-cols-2 gap-3 sm:gap-4">
          <img src="/img/about/reception.jpg" :alt="t('landing.home.introHeading')" loading="lazy" class="col-span-2 aspect-video rounded-xl object-cover">
          <img src="/img/about/lounge.jpg" alt="" loading="lazy" class="aspect-square rounded-xl object-cover">
          <img src="/img/about/consult.jpg" alt="" loading="lazy" class="aspect-square rounded-xl object-cover">
        </div>
      </div>
    </section>

    <!-- 둘러보기(2026-08-28, 사용자 지시로 원본 위젯 전체 재구현) — 원문 "원진성형외과 · 피부과
         둘러보기" 섹션. 층별(12~18F) 탭+이미지 캐러셀+시설 목록을 원본 인라인 스크립트(floorImages/
         floorMeta)를 그대로 읽어 HospitalFloorTour.vue로 재구현(무한루프 클론 슬라이드는 생략,
         단순 index 순환으로 충분 — 최소 구현). -->
    <section
      ref="tourTarget"
      class="border-y bg-muted/30 px-4 py-16 transition-all duration-700 sm:px-6 sm:py-20"
      :class="tourRevealed ? 'translate-y-0 opacity-100' : 'translate-y-6 opacity-0'"
    >
      <div class="mx-auto max-w-6xl">
        <div class="mb-10 text-center">
          <h2 class="mb-4 font-display text-2xl font-bold text-foreground sm:text-4xl">{{ t('landing.home.tourHeading') }}</h2>
          <p class="text-lg text-muted-foreground">{{ t('landing.home.tourBody') }}</p>
        </div>
        <HospitalFloorTour />
      </div>
    </section>

    <!-- 시설 소개 6가지(2026-08-28, 사용자 지시로 추가) — 원문 "프리미엄 안티에이징 센터" 등
         article.information 6개 카드(안티에이징·검진센터·마취과·안전시스템×2·편의시설). -->
    <section
      ref="centersTarget"
      class="mx-auto max-w-6xl px-4 py-16 transition-all duration-700 sm:px-6 sm:py-24"
      :class="centersRevealed ? 'translate-y-0 opacity-100' : 'translate-y-6 opacity-0'"
    >
      <div class="grid gap-8 sm:grid-cols-2">
        <div v-for="center in HOSPITAL_CENTERS" :key="center.slug" class="overflow-hidden rounded-xl border bg-card">
          <img :src="`/img/about/center/${center.image}`" :alt="center.title[locale as Locale]" loading="lazy" class="aspect-video w-full object-cover">
          <div class="p-6">
            <h3 class="mb-2 text-lg font-semibold text-foreground">{{ center.title[locale as Locale] }}</h3>
            <p class="text-sm text-muted-foreground">{{ center.desc[locale as Locale] }}</p>
          </div>
        </div>
      </div>
    </section>

    <!-- 1:1 맞춤 서비스 4가지(2026-08-28, 사용자 지시로 추가) — 원문 "전문적인 의료진과 플래너가
         1:1 맞춤형 고객 만족 시스템을 제공합니다." 섹션의 4개 특징 카드. -->
    <section
      ref="featuresTarget"
      class="mx-auto max-w-6xl px-4 py-16 transition-all duration-700 sm:px-6 sm:py-24"
      :class="featuresRevealed ? 'translate-y-0 opacity-100' : 'translate-y-6 opacity-0'"
    >
      <h2 class="mx-auto mb-12 max-w-3xl text-center font-display text-2xl font-bold text-foreground sm:text-4xl">{{ t('landing.home.serviceHeading') }}</h2>
      <div class="grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
        <div v-for="feature in FEATURES" :key="feature" class="rounded-xl border bg-card p-6">
          <h3 class="mb-2 text-lg font-semibold text-foreground">{{ t(`landing.home.${feature}Title`) }}</h3>
          <p class="text-sm text-muted-foreground">{{ t(`landing.home.${feature}Desc`) }}</p>
        </div>
      </div>
    </section>

    <!-- 하단 문의 CTA(2026-08-28, 사용자 지시로 리디자인) — 기존엔 올리브 밴드에 버튼 하나뿐이라
         빈약했다. 헤딩+보조문구+버튼 3단 구성 + 팔레트 그라디언트(primary→foreground)로 무게를
         주고, 끝색(foreground=짙은 산림녹)이 바로 아래 푸터(bg-foreground)로 자연스럽게 이어진다.
         fold 아래라 형제 섹션과 동일하게 스크롤 리빌 적용. -->
    <section
      ref="ctaTarget"
      class="bg-gradient-to-b from-primary to-foreground px-4 py-20 text-center transition-all duration-700 sm:px-6 sm:py-28"
      :class="ctaRevealed ? 'translate-y-0 opacity-100' : 'translate-y-6 opacity-0'"
    >
      <div class="mx-auto max-w-2xl">
        <h2 class="font-display text-3xl font-bold text-primary-foreground sm:text-5xl">{{ t('landing.home.ctaHeading') }}</h2>
        <p class="mt-4 text-lg text-primary-foreground/80">{{ t('landing.home.ctaBody') }}</p>
        <Button as-child size="lg" variant="secondary" class="mt-8">
          <NuxtLink :to="localePath('inquiry')">{{ t('procedures.inquireCta') }}</NuxtLink>
        </Button>
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
import { PROCEDURE_CATEGORIES, type Locale } from '~/data/procedures'
import { HOSPITAL_CENTERS } from '~/data/hospitalTour'

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

// 스크롤 리빌(8절) — fold 아래 섹션만 대상. 히어로는 above-the-fold라 대상에서 제외(화면 깜빡임 금지).
const { target: categoriesTarget, revealed: categoriesRevealed } = useScrollReveal()
const { target: introTarget, revealed: introRevealed } = useScrollReveal()
const { target: tourTarget, revealed: tourRevealed } = useScrollReveal()
const { target: centersTarget, revealed: centersRevealed } = useScrollReveal()
const { target: featuresTarget, revealed: featuresRevealed } = useScrollReveal()
const { target: ctaTarget, revealed: ctaRevealed } = useScrollReveal()

// 4가지 서비스 특징(landing.home.feature1~4 Title/Desc) — i18n 키 이름 패턴만 반복 참조, 데이터 아님.
const FEATURES = ['feature1', 'feature2', 'feature3', 'feature4']

// 🔴 UTM 캡처 + landing-visit 방문기록은 layouts/landing.vue로 옮겼다(최종 리뷰 발견 +
// 재검증에서 landing-visit 이전 누락 재지적) — 여기 홈에만 있으면 /procedures/eye/glam-eye?
// utm_source=... 같은 시술별 딥링크 광고가 /admin/referrals 방문집계에서 통째로 빠진다.
</script>
