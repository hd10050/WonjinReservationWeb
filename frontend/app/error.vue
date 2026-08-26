<template>
  <div class="flex min-h-screen flex-col items-center justify-center gap-4 bg-background px-6 text-center">
    <p class="text-6xl font-bold text-primary">{{ error?.statusCode ?? 500 }}</p>
    <h1 class="text-xl font-semibold text-foreground">{{ isNotFound ? t('errorPage.notFoundTitle') : t('errorPage.serverErrorTitle') }}</h1>
    <p class="max-w-sm text-sm text-muted-foreground">{{ isNotFound ? t('errorPage.notFoundMessage') : t('errorPage.serverErrorMessage') }}</p>
    <Button class="mt-2" @click="goHome">{{ t('errorPage.goHome') }}</Button>
  </div>
</template>

<script setup lang="ts">
// 404/500 통합 에러 화면(U3, 12-1절) — 상태코드별 문구만 바꾸고 화면을 나누지 않는다.
// 관리자 경로에서 난 에러도 이 화면을 그대로 쓴다 — 별도 관리자용 에러 페이지는 만들지 않는다.
import type { NuxtError } from '#app'

const props = defineProps<{ error: NuxtError }>()

useHead({ meta: [{ name: 'robots', content: 'noindex, nofollow' }] })

const { t } = useI18n()
const localePath = useLocalePath()
const isNotFound = computed(() => props.error?.statusCode === 404)

// 현재 로케일을 유지한 채 홈으로 — clearError로 Nuxt 에러 상태를 먼저 정리해야
// 다음 네비게이션이 다시 에러 화면에 걸리지 않는다.
function goHome() {
  clearError({ redirect: localePath('/') })
}
</script>
