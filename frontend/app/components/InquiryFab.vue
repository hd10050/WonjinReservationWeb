<!-- frontend/app/components/InquiryFab.vue -->
<template>
  <NuxtLink
    :to="localePath('inquiry')"
    :aria-label="t('landing.nav.inquiryFab')"
    class="fixed right-5 bottom-5 z-40 flex items-center gap-2 rounded-full bg-primary px-4 py-3 text-sm font-semibold text-primary-foreground shadow-lg transition-transform hover:scale-105"
  >
    <MessageCircle class="size-5 shrink-0" />
    <span class="hidden sm:inline">{{ t('landing.nav.inquiryFab') }}</span>
  </NuxtLink>
</template>

<script setup lang="ts">
import { MessageCircle } from '@lucide/vue'

const { t } = useI18n()
const localePath = useLocalePath()
</script>
