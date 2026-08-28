<!-- frontend/app/components/HospitalFloorTour.vue -->
<!-- 층별 시설 둘러보기(2026-08-28, 사용자 지시로 k-wonjin.co.kr/hospitalinfo/about 원본 위젯 재구현).
     원본은 무한루프 슬라이더(클론 슬라이드)지만 이번엔 단순 index 순환으로 충분(최소 구현). -->
<template>
  <div>
    <div class="mb-8 flex flex-wrap justify-center gap-2">
      <button
        v-for="f in floors"
        :key="f.floor"
        type="button"
        class="rounded-md border px-4 py-2 text-sm font-semibold transition-colors"
        :class="activeFloor === f.floor ? 'border-primary bg-primary text-primary-foreground' : 'text-muted-foreground hover:border-primary hover:text-foreground'"
        @click="selectFloor(f.floor)"
      >
        {{ f.floor }}F
      </button>
    </div>

    <div class="grid gap-8 sm:grid-cols-2 sm:items-center">
      <div class="group relative aspect-[4/3] overflow-hidden rounded-xl bg-muted">
        <img
          v-for="(img, i) in activeFloorData.images"
          :key="img"
          :src="`/img/about/floors/${img}`"
          :alt="`${activeFloor}F`"
          loading="lazy"
          class="absolute inset-0 size-full object-cover transition-opacity duration-500"
          :class="i === activeImageIndex ? 'opacity-100' : 'opacity-0'"
        >
        <button
          type="button"
          :aria-label="t('common.prev')"
          class="absolute left-2 top-1/2 flex size-8 -translate-y-1/2 items-center justify-center rounded-full bg-black/40 text-white opacity-0 transition-opacity group-hover:opacity-100"
          @click="prevImage"
        >
          <ChevronLeft class="size-5" />
        </button>
        <button
          type="button"
          :aria-label="t('common.next')"
          class="absolute right-2 top-1/2 flex size-8 -translate-y-1/2 items-center justify-center rounded-full bg-black/40 text-white opacity-0 transition-opacity group-hover:opacity-100"
          @click="nextImage"
        >
          <ChevronRight class="size-5" />
        </button>
        <div class="absolute inset-x-0 bottom-3 flex justify-center gap-1.5">
          <button
            v-for="(img, i) in activeFloorData.images"
            :key="img"
            type="button"
            :aria-label="`${i + 1}`"
            class="size-1.5 rounded-full transition-colors"
            :class="i === activeImageIndex ? 'bg-white' : 'bg-white/40'"
            @click="activeImageIndex = i"
          />
        </div>
      </div>

      <div>
        <p class="mb-4 font-display text-3xl font-bold text-foreground">{{ activeFloor }}F</p>
        <ul class="space-y-3">
          <li v-for="item in activeFloorData.items[locale as Locale]" :key="item" class="flex items-center gap-2 text-muted-foreground">
            <Check class="size-4 shrink-0 text-primary" />
            <span>{{ item }}</span>
          </li>
        </ul>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { Check, ChevronLeft, ChevronRight } from '@lucide/vue'
import { HOSPITAL_FLOORS } from '~/data/hospitalTour'
import type { Locale } from '~/data/procedures'

const { t, locale } = useI18n()
const floors = HOSPITAL_FLOORS
const activeFloor = ref(floors[0].floor)
const activeImageIndex = ref(0)

const activeFloorData = computed(() => floors.find(f => f.floor === activeFloor.value) ?? floors[0])

function selectFloor(floor: number) {
  activeFloor.value = floor
  activeImageIndex.value = 0
}
function nextImage() {
  const len = activeFloorData.value.images.length
  activeImageIndex.value = (activeImageIndex.value + 1) % len
}
function prevImage() {
  const len = activeFloorData.value.images.length
  activeImageIndex.value = (activeImageIndex.value - 1 + len) % len
}

let timer: ReturnType<typeof setInterval> | undefined
onMounted(() => {
  if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) return
  timer = setInterval(nextImage, 3000)
})
onUnmounted(() => {
  if (timer) clearInterval(timer)
})
</script>
