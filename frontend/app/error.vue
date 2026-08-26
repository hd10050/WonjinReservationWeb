<template>
  <div class="flex min-h-screen flex-col items-center justify-center gap-4 bg-background px-4 text-center">
    <p class="text-3xl font-semibold text-foreground">{{ error.statusCode }}</p>
    <p class="text-muted-foreground">{{ message }}</p>
    <Button @click="goHome">{{ t('error.home') }}</Button>
  </div>
</template>

<script setup lang="ts">
// U3 — 404/500을 하나의 화면으로 함께 처리한다(상태코드별 문구만 분기). 관리자 경로 에러도
// 이 화면을 쓴다(12-1절) — 레이아웃 없이 홈 링크 하나만 두는 최소 구성.
import type { NuxtError } from '#app'

const props = defineProps<{ error: NuxtError }>()

useHead({ meta: [{ name: 'robots', content: 'noindex, nofollow' }] })

const { t } = useI18n()
const localePath = useLocalePath()

const message = computed(() => (props.error.statusCode === 404 ? t('error.notFound') : t('error.serverError')))

function goHome() {
  clearError({ redirect: localePath('index') })
}
</script>
