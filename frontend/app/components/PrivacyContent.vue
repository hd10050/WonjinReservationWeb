<template>
  <div class="space-y-6">
    <p class="text-sm text-muted-foreground">{{ t('privacy.intro') }}</p>
    <div v-for="(s, i) in sections" :key="i" class="space-y-1.5">
      <h2 class="font-semibold text-foreground">{{ rt(s.heading) }}</h2>
      <p class="text-sm whitespace-pre-line text-muted-foreground">{{ rt(s.body) }}</p>
    </div>
    <p class="text-xs text-muted-foreground">{{ t('privacy.lastUpdatedLabel') }}: {{ t('privacy.lastUpdated') }}</p>
  </div>
</template>

<script setup lang="ts">
// privacy.vue(전체 페이지)와 예약 폼 모달이 이 컴포넌트 하나를 공유한다 — 문안 이중 관리 방지.
const { t, tm, rt } = useI18n()
const sections = computed(() => tm('privacy.sections') as { heading: unknown, body: unknown }[])
</script>
