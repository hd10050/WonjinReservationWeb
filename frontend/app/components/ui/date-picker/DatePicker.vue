<script setup lang="ts">
import type { HTMLAttributes } from 'vue'
import type { DateValue } from '@internationalized/date'
import { getLocalTimeZone, parseDate, today } from '@internationalized/date'
import { CalendarIcon, X } from '@lucide/vue'
import { createYearRange } from 'reka-ui/date'
import { Button } from '@/components/ui/button'
import { Calendar } from '@/components/ui/calendar'
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover'
import { cn } from '@/lib/utils'

// D11 대체 — shadcn Popover+Calendar(reka-ui 기반) 커스텀 날짜 선택기. v-model은 기존 네이티브
// <input type="date">와 동일하게 "YYYY-MM-DD" 문자열(또는 빈 문자열)만 주고받는다 — 제출 로직·API
// 계약은 그대로 두고 위젯만 교체하기 위함. locale은 호출부의 useInputLang()(BCP-47)을 그대로 받아
// 팝업 캘린더 요일·월 이름까지 코드로 제어한다(9-2절① 잔여 한계 해소).
const props = withDefaults(defineProps<{
  modelValue: string
  locale: string
  disabled?: boolean
  invalid?: boolean
  placeholder?: string
  minValue?: DateValue
  maxValue?: DateValue
  class?: HTMLAttributes['class']
}>(), {
  disabled: false,
  invalid: false,
})
const emit = defineEmits<{ (e: 'update:modelValue', value: string): void }>()

// 🔴 UX(2026-08-28) — 화살표로만 월 이동이 가능해 생년월일처럼 먼 과거를 고를 때 수십~수백 번
// 클릭해야 했음. app/components/ui/calendar/Calendar.vue에 이미 구현된 shadcn "month-and-year"
// 레이아웃(년/월 네이티브 select, D11 대체 당시 함께 들여온 것)을 활성화하는 것으로 해결 — 타이핑
// 입력으로 바꾸지 않음. year-range를 명시적으로 고정해두지 않으면 Calendar.vue가 매번 현재
// placeholder(=화면에 보이는 달) 기준 -100/+10년으로 재계산해, 연도를 선택할 때마다 선택지 목록
// 자체가 다시 움직여버린다 — "오늘" 한 번만 앵커로 고정.
const yearRange = createYearRange({
  start: today(getLocalTimeZone()).cycle('year', -100),
  end: today(getLocalTimeZone()).cycle('year', 10),
})

const { t } = useI18n()
const open = ref(false)

const calendarValue = computed<DateValue | undefined>({
  get: () => (props.modelValue ? parseDate(props.modelValue) : undefined),
  set: (v) => {
    emit('update:modelValue', v ? v.toString() : '')
    open.value = false
  },
})

// 표시 형식은 timeZone:'UTC' 고정 — date 컬럼 값(타임존 없음)을 브라우저 로컬 타임존으로 포맷하면
// UTC보다 뒤쪽 타임존(예: 미국)에서 하루 밀려 보일 수 있다(9-2절②와 동일한 이유의 별도 적용).
const displayLabel = computed(() => {
  if (!calendarValue.value) return ''
  return new Intl.DateTimeFormat(props.locale, { year: 'numeric', month: 'long', day: 'numeric', timeZone: 'UTC' })
    .format(calendarValue.value.toDate('UTC'))
})

function clear() {
  emit('update:modelValue', '')
}
</script>

<template>
  <div class="relative" :class="cn('w-[200px]', props.class)">
    <Popover v-model:open="open">
      <PopoverTrigger as-child>
        <Button
          type="button"
          variant="outline"
          :disabled="disabled"
          :aria-invalid="invalid"
          :class="cn(
            'w-full justify-start text-left font-normal',
            !modelValue ? 'text-muted-foreground' : '',
            invalid ? 'border-destructive ring-destructive/20 dark:ring-destructive/40' : '',
          )"
        >
          <CalendarIcon class="mr-2 h-4 w-4 shrink-0 opacity-60" />
          <span class="flex-1 truncate">{{ displayLabel || placeholder || t('common.pickDate') }}</span>
          <span v-if="modelValue && !disabled" class="w-4 shrink-0" aria-hidden="true" />
        </Button>
      </PopoverTrigger>
      <PopoverContent class="w-auto p-0" align="start">
        <!-- 🔴 버그(2026-08-28 사용자 지적) — PopoverContent는 닫힐 때 Calendar를 언마운트한다.
             default-placeholder 없이는 Calendar.vue의 내부 placeholder(표시 월)가 매번 today()로
             새로 초기화돼, 이미 값을 골라둔 상태에서 다시 열어도 항상 이번 달 달력부터 보였다(실측
             확인 — 선택된 날짜 자체(calendarValue)는 유지되지만 그 달로 스크롤은 안 됐음). 재오픈
             시점의 현재 값을 넘겨 그 달부터 열리게 한다 — 값이 없으면 Calendar.vue가 today()로 폴백. -->
        <Calendar
          v-model="calendarValue"
          :default-placeholder="calendarValue"
          :locale="locale"
          :min-value="minValue"
          :max-value="maxValue"
          layout="month-and-year"
          :year-range="yearRange"
        />
      </PopoverContent>
    </Popover>
    <button
      v-if="modelValue && !disabled"
      type="button"
      :aria-label="t('common.clear')"
      class="absolute top-1/2 right-2 -translate-y-1/2 rounded-sm text-muted-foreground opacity-60 outline-none hover:opacity-100 focus-visible:opacity-100"
      @click.stop="clear"
    >
      <X class="h-3.5 w-3.5" />
    </button>
  </div>
</template>
