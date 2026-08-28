<!-- frontend/app/components/ProcedureMedicalSystem.vue -->
<!-- 시술 카테고리 목록 페이지의 "고민이 있으신가요?" 영역 위에 붙는 병원 시스템·특장점 섹션
     (2026-08-28, 사용자 지시로 k-wonjin.co.kr 각 카테고리 마케팅 섹션 이관). 데이터는
     data/procedureMedical.ts. 블록 타입(intro/steps/quote/features/gallery)별로 렌더링.
     블록마다 스크롤 진입 시 순차(스타거) 페이드업 — 형제 섹션과 동일한 useScrollReveal 패턴,
     revealed 상태값에 직접 클래스/딜레이 바인딩(트랜지션 중단돼도 상태와 항상 일치). -->
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
          <p class="text-muted-foreground sm:text-lg" :class="{ 'mt-4': block.headline || block.subhead }">
            {{ block.body[locale as Locale] }}
          </p>
        </div>

        <!-- 01·02·03 번호 카드 -->
        <div v-else-if="block.type === 'steps'" class="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <div v-for="s in block.items" :key="s.no" class="rounded-xl border bg-card p-5">
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
        <div
          v-else-if="block.type === 'features'"
          class="grid gap-6 sm:grid-cols-2"
          :class="{ 'lg:grid-cols-3': block.items.length >= 5 }"
        >
          <div
            v-for="(f, fi) in block.items"
            :key="fi"
            class="flex flex-col items-center rounded-xl border bg-card p-6 text-center"
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

        <!-- 이미지 갤러리 (+ 선택 캡션) — 인증서·논문·수상 등 -->
        <div v-else-if="block.type === 'gallery'">
          <div class="grid grid-cols-2 gap-3 sm:grid-cols-4 lg:grid-cols-5">
            <img
              v-for="img in block.images"
              :key="img"
              :src="`/img/${img}`"
              :alt="block.caption ? block.caption[locale as Locale] : ''"
              loading="lazy"
              class="aspect-[3/4] w-full rounded-lg border bg-card object-contain p-1"
            >
          </div>
          <p v-if="block.caption" class="mt-3 text-center text-sm text-muted-foreground">
            {{ block.caption[locale as Locale] }}
          </p>
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
</script>
