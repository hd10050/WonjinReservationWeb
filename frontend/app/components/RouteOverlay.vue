<template>
  <!--
    🔴 v-if 없이 항상 마운트해둔 채 pointer-events·투명도를 상태값에 직접 클래스 바인딩으로만 토글한다(13-2절).
    <Transition>로 마운트/언마운트하면: 빠른 연속 전환(연타) 시 leave가 끝나기 전 enter가 걸려 트랜지션이
    깨지고, opacity:0인 이 엘리먼트가 DOM에 영구히 남아 사이트 전체 클릭이 전부 씹히는 상태가 된다(실제 사고).
  -->
  <div
    class="fixed inset-0 z-50 flex items-center justify-center bg-white/60 transition-opacity duration-150"
    :class="active ? 'opacity-100 pointer-events-auto' : 'opacity-0 pointer-events-none'"
    aria-hidden="true"
  >
    <Loader2 class="size-10 animate-spin text-primary" />
  </div>
</template>

<script setup lang="ts">
import { Loader2 } from '@lucide/vue'

// 🔴 page:start/page:finish(Suspense pending/resolve)는 전환이 다른 전환에 가로채이면 start만 다시 찍히고
// 그 전 전환의 finish는 영영 안 와서 카운터가 0으로 안 돌아온다(실사용 재현 — 로딩 중 다른 링크 클릭 시 오버레이 고착).
// page:loading:start/end는 router.beforeEach/afterEach 기반이라 취소된 전환도 실패(failure)로 빠짐없이
// page:loading:end를 별도로 호출해준다(Nuxt 내장 NuxtLoadingIndicator와 동일 메커니즘, 카운터 아닌 상태값이라 안전).
// throttle 기본값(200ms)은 진행률바 깜빡임 방지용 — 이 오버레이는 클릭 차단이 목적이라 지연 없이 즉시 켜야 함.
const { isLoading } = useLoadingIndicator({ throttle: 0 })
const active = isLoading
</script>
