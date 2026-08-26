<template>
  <div class="mx-auto max-w-3xl px-4 py-10">
    <h1 class="mb-4 text-2xl font-semibold text-foreground">{{ t('privacy.title') }}</h1>
    <p class="text-muted-foreground">{{ t('privacy.pending') }}</p>
  </div>
</template>

<script setup lang="ts">
// 개인정보 처리방침 — 문안·보유기간은 범위 외(20-1절), 페이지 틀만 만든다(12-1절).
definePageMeta({ layout: 'landing' })
const { t } = useI18n()
useSeo({ title: () => t('privacy.title') })
</script>
