<!-- frontend/app/components/ProcedureMedicalSystem.vue -->
<!-- 시술 카테고리 목록 페이지의 "고민이 있으신가요?" 영역 위에 붙는 병원 시스템·특장점 섹션
     (2026-08-28, 사용자 지시로 k-wonjin.co.kr 각 카테고리 마케팅 섹션 이관). 데이터는
     data/procedureMedical.ts. 블록 타입(intro/steps/quote/features)별로 렌더링.
     블록마다 스크롤 진입 시 순차(스타거) 페이드업 — 형제 섹션과 동일한 useScrollReveal 패턴,
     revealed 상태값에 직접 클래스/딜레이 바인딩(트랜지션 중단돼도 상태와 항상 일치).
     카드 행은 grid가 아니라 flex-wrap+justify-center — 마지막 줄이 꽉 안 차도 가운데 정렬돼
     왼쪽에 붙은 고아 카드가 안 생긴다(3+2, 4+2 등에서 정렬이 깨지던 문제 해결). -->
<template>
  <section ref="target" class="border-b bg-muted/30 px-4 py-16 sm:px-6 sm:py-24">
    <div class="mx-auto max-w-6xl">
      <div
        v-for="(block, bi) in blocks"
        :key="bi"
        class="transition-all duration-700"
        :class="[
          revealed ? 'translate-y-0 opacity-100' : 'translate-y-6 opacity-0',
          bi > 0 ? 'mt-12 sm:mt-16' : '',
        ]"
        :style="{ transitionDelay: revealed ? `${Math.min(bi, 6) * 120}ms` : '0ms' }"
      >
        <!-- 헤드라인(선택) + 오버라인(선택) + 본문 -->
        <div v-if="block.type === 'intro'" class="mx-auto max-w-3xl text-center">
          <p v-if="block.subhead" class="mb-2 text-sm font-semibold uppercase tracking-widest text-primary">
            {{ block.subhead[locale as Locale] }}
          </p>
          <h2 v-if="block.headline" class="font-display text-2xl font-bold text-foreground sm:text-4xl">
            {{ block.headline[locale as Locale] }}
          </h2>
          <p class="whitespace-pre-line text-muted-foreground sm:text-lg" :class="{ 'mt-4': block.headline || block.subhead }">
            {{ block.body[locale as Locale] }}
          </p>
        </div>

        <!-- 01·02·03 번호 카드 -->
        <div v-else-if="block.type === 'steps'" class="flex flex-wrap justify-center gap-4">
          <div
            v-for="s in block.items"
            :key="s.no"
            class="rounded-xl border bg-card p-5"
            :class="stepWidth(block.items.length)"
          >
            <span class="font-display text-2xl font-bold text-primary/60">{{ s.no }}</span>
            <p class="mt-2 text-sm font-medium text-foreground">{{ s.text[locale as Locale] }}</p>
          </div>
        </div>

        <!-- 강조 인용문 (+ 선택 출처) -->
        <blockquote v-else-if="block.type === 'quote'" class="mx-auto max-w-3xl border-l-4 border-primary pl-6">
          <p class="text-lg font-medium text-foreground sm:text-xl">{{ block.text[locale as Locale] }}</p>
          <cite v-if="block.cite" class="mt-3 block text-xs not-italic text-muted-foreground">
            {{ block.cite[locale as Locale] }}
          </cite>
        </blockquote>

        <!-- 제목+본문(+번호·이미지) 카드 -->
        <div v-else-if="block.type === 'features'" class="flex flex-wrap justify-center gap-6">
          <div
            v-for="(f, fi) in block.items"
            :key="fi"
            class="flex flex-col items-center rounded-xl border bg-card p-6 text-center"
            :class="featureWidth(block.items.length)"
          >
            <img
              v-if="f.image"
              :src="`/img/${f.image}`"
              :alt="f.title[locale as Locale]"
              loading="lazy"
              class="mb-4"
              :class="f.imageFit === 'contain'
                ? 'h-36 w-full rounded-lg object-contain'
                : 'size-36 rounded-full object-cover'"
            >
            <span v-if="f.no" class="font-display text-xl font-bold text-primary/60">{{ f.no }}</span>
            <h3 class="text-lg font-semibold text-foreground" :class="{ 'mt-1': f.no }">{{ f.title[locale as Locale] }}</h3>
            <p class="mt-2 text-sm text-muted-foreground">{{ f.body[locale as Locale] }}</p>
          </div>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup lang="ts">
import type { MedicalBlock } from '~/data/procedureMedical'
import type { Locale } from '~/data/procedures'

defineProps<{ blocks: MedicalBlock[] }>()

const { locale } = useI18n()
const { target, revealed } = useScrollReveal()

// flex-basis를 gap(gap-6=1.5rem)에 맞춰 계산 — 마지막 줄은 justify-center로 가운데 정렬된다.
// 4개짜리 블록은 2+2가 자연스러우므로 2단까지만, 그 외(3·5·6…)는 3단까지 허용.
function featureWidth(n: number): string {
  const twoUp = 'w-full sm:w-[calc(50%-0.75rem)]'
  return n === 2 || n === 4 ? twoUp : `${twoUp} lg:w-[calc(33.333%-1rem)]`
}
// steps는 gap-4(1rem). 5개 이상이면 3단(6→3+3), 그 이하는 4단(4→한 줄).
function stepWidth(n: number): string {
  const twoUp = 'w-full sm:w-[calc(50%-0.5rem)]'
  return n > 4 ? `${twoUp} lg:w-[calc(33.333%-0.667rem)]` : `${twoUp} lg:w-[calc(25%-0.75rem)]`
}
</script>
