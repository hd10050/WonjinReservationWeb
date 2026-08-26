<template>
  <div class="flex min-h-screen flex-col items-center justify-center gap-4 bg-background">
    <NuxtRouteAnnouncer />
    <p class="text-2xl font-semibold text-foreground">{{ t('common.appName') }}</p>
    <div class="flex gap-2">
      <Button>Primary</Button>
      <Button variant="secondary">Secondary</Button>
      <Button variant="destructive">Destructive</Button>
      <Button variant="outline">Outline</Button>
    </div>
  </div>
</template>

<script setup lang="ts">
// Phase 0 스캐폴딩 검증용 placeholder — Phase 2에서 실제 4언어 랜딩(12-1절)으로 교체된다.
const { t } = useI18n()
</script>
