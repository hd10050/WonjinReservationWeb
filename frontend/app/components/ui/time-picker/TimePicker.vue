<script setup lang="ts">
import type { HTMLAttributes } from 'vue'
import { Time } from '@internationalized/date'
import { TimeFieldInput, TimeFieldRoot } from 'reka-ui'
import { cn } from '@/lib/utils'

// D11 대체 — reka-ui TimeField(시:분 세그먼트 입력) 커스텀 시간 선택기. v-model은 기존 네이티브
// <input type="time">와 동일하게 "HH:mm" 문자열(또는 빈 문자열)만 주고받는다. 24시간제로 고정해
// 로케일별 오전/오후 표기 차이를 없앤다(병원 예약 맥락상 일관된 24시간제가 더 명확함).
const props = withDefaults(defineProps<{
  modelValue: string
  locale: string
  disabled?: boolean
  invalid?: boolean
  class?: HTMLAttributes['class']
}>(), {
  disabled: false,
  invalid: false,
})
const emit = defineEmits<{ (e: 'update:modelValue', value: string): void }>()

function parseHm(v: string): Time | undefined {
  if (!v) return undefined
  const [h, m] = v.split(':').map(Number)
  return new Time(h, m)
}
function formatHm(v: Time | undefined): string {
  if (!v) return ''
  return `${String(v.hour).padStart(2, '0')}:${String(v.minute).padStart(2, '0')}`
}

const timeValue = computed<Time | undefined>({
  get: () => parseHm(props.modelValue),
  set: v => emit('update:modelValue', formatHm(v)),
})
</script>

<template>
  <TimeFieldRoot
    v-model="timeValue"
    :locale="locale"
    hour-cycle="h23"
    :disabled="disabled"
    :aria-invalid="invalid"
    granularity="minute"
    :class="cn(
      'border-input flex h-9 w-fit items-center gap-0.5 rounded-md border bg-transparent px-3 text-sm shadow-xs',
      'focus-within:border-ring focus-within:ring-ring/50 focus-within:ring-3',
      disabled ? 'cursor-not-allowed opacity-50' : '',
      invalid ? 'border-destructive ring-destructive/20 dark:ring-destructive/40' : '',
      props.class,
    )"
  >
    <TimeFieldInput part="hour" class="rounded px-0.5 text-center tabular-nums outline-none focus:bg-accent" />
    <span class="text-muted-foreground">:</span>
    <TimeFieldInput part="minute" class="rounded px-0.5 text-center tabular-nums outline-none focus:bg-accent" />
  </TimeFieldRoot>
</template>
