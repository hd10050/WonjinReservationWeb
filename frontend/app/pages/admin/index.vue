<template>
  <div>
    <h1 class="text-xl font-semibold text-foreground">{{ t('admin.index.welcome') }}, {{ user?.name }}</h1>
  </div>
</template>

<script setup lang="ts">
// Phase 3에서 실제 예약 대시보드(12-4절, 4개 상태 카드 + 목록/필터/페이징)로 교체될 자리 표시자.
definePageMeta({ middleware: 'admin', layout: 'admin', i18n: false })

const { t } = useI18n()
const { user } = useAuth()
</script>
