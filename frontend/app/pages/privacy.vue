<template>
  <div class="mx-auto max-w-3xl px-4 py-10">
    <h1 class="mb-6 text-2xl font-semibold text-foreground">{{ t('privacy.title') }}</h1>
    <PrivacyContent />
  </div>
</template>

<script setup lang="ts">
// 개인정보 처리방침 — 예문 작성(2026-08-27, 20-1절 갱신). 법률 검토를 거친 최종본은 별도로 대체될 수 있다.
definePageMeta({ layout: 'landing' })
const { t } = useI18n()
useSeo({ title: () => t('privacy.title') })
</script>
