<template>
  <!--
    🔴 v-if 없이 항상 마운트해둔 채 pointer-events·투명도를 상태값에 직접 클래스 바인딩으로만 토글한다(13-2절).
    <Transition>로 마운트/언마운트하면: 빠른 연속 전환(연타) 시 leave가 끝나기 전 enter가 걸려 트랜지션이
    깨지고, opacity:0인 이 엘리먼트가 DOM에 영구히 남아 사이트 전체 클릭이 전부 씹히는 상태가 된다(실제 사고).
  -->
  <div
    class="fixed inset-0 z-50 bg-white/60 transition-opacity duration-150"
    :class="active ? 'opacity-100 pointer-events-auto' : 'opacity-0 pointer-events-none'"
    aria-hidden="true"
  />
</template>

<script setup lang="ts">
const { pending } = useRouteOverlay()
const active = computed(() => pending.value > 0)
</script>
