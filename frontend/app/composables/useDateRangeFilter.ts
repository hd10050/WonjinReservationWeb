import type { Ref } from 'vue'
import type { DateValue } from '@internationalized/date'
import { parseDate } from '@internationalized/date'

// 검색필터 시작일~종료일(YYYY-MM-DD 고정 길이) 공용 가드. 종료일 DatePicker에 이 toMinValue를
// :min-value로 넘기면 캘린더에서 시작일 이전 날짜 자체가 비활성화된다(1차 방어). 시작일을 종료일
// 뒤로 옮기는 등 그 경로로 역전되면 캘린더 비활성화만으로는 못 막으므로, 역전 상태가 되는 즉시
// 종료일=시작일로 맞춘다(2차 방어) — 문자열이 YYYY-MM-DD 고정 길이라 사전식 비교가 곧 날짜 비교.
export function useDateRangeFilter(from: Ref<string>, to: Ref<string>) {
  const toMinValue = computed<DateValue | undefined>(() => (from.value ? parseDate(from.value) : undefined))

  watch([from, to], ([f, t]) => {
    if (f && t && t < f) to.value = f
  })

  return { toMinValue }
}
