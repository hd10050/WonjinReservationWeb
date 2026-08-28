<!-- frontend/app/components/ProcedureMedicalSystem.vue -->
<!-- 시술 카테고리 목록 페이지의 "고민이 있으신가요?" 영역 위에 붙는 병원 시스템·특장점 섹션
     (2026-08-28, 사용자 지시로 k-wonjin.co.kr 각 카테고리 마케팅 섹션 이관). 데이터는
     data/procedureMedical.ts. 블록 타입(intro/steps/quote/features)별로 렌더링. fold 아래라
     형제 섹션과 동일하게 스크롤 리빌 적용. -->
<template>
  <section
    ref="target"
    class="border-b bg-muted/30 px-4 py-16 transition-all duration-700 sm:px-6 sm:py-24"
    :class="revealed ? 'translate-y-0 opacity-100' : 'translate-y-6 opacity-0'"
  >
    <div class="mx-auto max-w-6xl">
      <template v-for="(block, bi) in blocks" :key="bi">
        <!-- 헤드라인 + 오버라인 + 본문 -->
        <div v-if="block.type === 'intro'" class="mx-auto max-w-3xl text-center" :class="{ 'mt-16': bi > 0 }">
          <p v-if="block.subhead" class="mb-2 text-sm font-semibold uppercase tracking-widest text-primary">
            {{ block.subhead[locale as Locale] }}
          </p>
          <h2 class="font-display text-2xl font-bold text-foreground sm:text-4xl">{{ block.headline[locale as Locale] }}</h2>
          <p class="mt-4 text-muted-foreground sm:text-lg">{{ block.body[locale as Locale] }}</p>
        </div>

        <!-- 01·02·03 번호 카드 -->
        <div v-else-if="block.type === 'steps'" class="mt-10 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <div v-for="s in block.items" :key="s.no" class="rounded-xl border bg-card p-5">
            <span class="font-display text-2xl font-bold text-primary/60">{{ s.no }}</span>
            <p class="mt-2 text-sm font-medium text-foreground">{{ s.text[locale as Locale] }}</p>
          </div>
        </div>

        <!-- 강조 인용문 -->
        <blockquote
          v-else-if="block.type === 'quote'"
          class="mx-auto mt-12 max-w-3xl border-l-4 border-primary pl-6 text-lg font-medium text-foreground sm:text-xl"
        >
          {{ block.text[locale as Locale] }}
        </blockquote>

        <!-- 제목+본문(+이미지) 카드 -->
        <div v-else-if="block.type === 'features'" class="mt-12 grid gap-8 sm:grid-cols-2">
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
              class="mb-4 size-40 rounded-full object-cover"
            >
            <h3 class="text-lg font-semibold text-foreground">{{ f.title[locale as Locale] }}</h3>
            <p class="mt-2 text-sm text-muted-foreground">{{ f.body[locale as Locale] }}</p>
          </div>
        </div>
      </template>
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
