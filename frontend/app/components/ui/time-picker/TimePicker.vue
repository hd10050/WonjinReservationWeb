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
  <!-- 🔴 성능/버그(2026-08-27, "시간 UI가 작동 안 함" 재조사) — TimeFieldInput은 자체적으로 값을
       표시하지 않는다(내부는 contenteditable div + <slot />뿐). Context7(reka-ui 공식 문서)로 확인한
       실제 사용 패턴은 TimeFieldRoot가 노출하는 segments 배열을 v-for로 돌며 각 세그먼트의 value를
       슬롯 콘텐츠로 직접 넣는 것 — 이전 코드는 <TimeFieldInput part="hour" />를 자식 없이 썼어서
       세그먼트 값은 내부적으로(aria-valuenow 등) 정상 갱신되는데도 화면엔 영원히 빈 칸만 보였다
       (실측: 합성 키보드 이벤트로 aria-valuenow는 바뀌는데 textContent는 항상 "" — 브라우저 pane
       클릭이 무반응이라 "작동 안 함"으로 보이는 것과는 다른, 진짜 렌더링 누락). -->
  <TimeFieldRoot
    v-slot="{ segments }"
    v-model="timeValue"
    :locale="locale"
    :hour-cycle="24"
    :disabled="disabled"
    :aria-invalid="invalid"
    granularity="minute"
    :class="cn(
      'border-input flex h-9 w-32 items-center gap-0.5 rounded-md border bg-transparent px-3 text-sm shadow-xs',
      'focus-within:border-ring focus-within:ring-ring/50 focus-within:ring-3',
      disabled ? 'cursor-not-allowed opacity-50' : '',
      invalid ? 'border-destructive ring-destructive/20 dark:ring-destructive/40' : '',
      props.class,
    )"
  >
    <template v-for="segment in segments" :key="segment.part">
      <!-- 🔴 실측(2026-08-27) — granularity="minute"인데도 라이브러리가 로케일에 따라 segments에
           dayPeriod("AM"/"PM") 파트를 함께 내보낸다(ko-KR 확인). 24시간제 고정 취지(위 주석)를
           지키려면 hour/minute/literal 세 파트만 그리고 dayPeriod는 명시적으로 걸러야 한다. -->
      <span v-if="segment.part === 'literal'" class="text-muted-foreground">{{ segment.value }}</span>
      <TimeFieldInput
        v-else-if="segment.part === 'hour' || segment.part === 'minute'"
        :part="segment.part"
        class="rounded px-0.5 text-center tabular-nums outline-none focus:bg-accent"
      >
        {{ segment.value }}
      </TimeFieldInput>
    </template>
  </TimeFieldRoot>
</template>
