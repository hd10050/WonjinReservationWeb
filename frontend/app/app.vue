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
const { t } = useI18n()
</script>
