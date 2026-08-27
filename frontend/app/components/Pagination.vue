<template>
  <div class="flex flex-wrap items-center justify-center gap-1.5">
    <Button variant="outline" size="icon-sm" :disabled="page <= 1" aria-label="이전 5페이지" @click="go(page - 5)">«</Button>
    <Button variant="outline" size="sm" :disabled="page <= 1" @click="go(page - 1)">{{ t('admin.reservations.prev') }}</Button>
    <Button
      v-for="p in pageNumbers" :key="p"
      :variant="p === page ? 'default' : 'outline'"
      size="icon-sm"
      @click="go(p)"
    >{{ p }}</Button>
    <Button variant="outline" size="sm" :disabled="page >= totalPages" @click="go(page + 1)">{{ t('admin.reservations.next') }}</Button>
    <Button variant="outline" size="icon-sm" :disabled="page >= totalPages" aria-label="다음 5페이지" @click="go(page + 5)">»</Button>
  </div>
</template>

<script setup lang="ts">
const props = defineProps<{ page: number, totalPages: number }>()
const emit = defineEmits<{ (e: 'update:page', page: number): void }>()

const { t } = useI18n()

// VixWeb Pagination.vue와 동일한 5개 단위 윈도 계산(참고 구현) — 팔레트·Button 컴포넌트만 이 프로젝트 것으로 교체
const pageNumbers = computed(() => {
  const total = Math.max(1, props.totalPages)
  const start = Math.floor((props.page - 1) / 5) * 5 + 1
  const end = Math.min(total, start + 4)
  const arr: number[] = []
  for (let p = start; p <= end; p++) arr.push(p)
  return arr
})

function go(target: number) {
  const clamped = Math.min(Math.max(target, 1), Math.max(1, props.totalPages))
  if (clamped !== props.page) emit('update:page', clamped)
}
</script>
