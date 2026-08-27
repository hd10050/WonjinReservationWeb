<template>
  <div class="space-y-6">
    <h1 class="text-xl font-semibold text-foreground">{{ t('admin.calendar.title') }}</h1>

    <div class="grid grid-cols-1 gap-6 lg:grid-cols-[1fr_320px]">
      <Card>
        <CardContent>
          <div class="mb-4 flex flex-wrap items-end justify-between gap-3">
            <div class="flex items-center gap-2">
              <Button variant="outline" size="sm" @click="goMonth(-1)">{{ t('admin.calendar.prevMonth') }}</Button>
              <span class="font-semibold text-foreground">{{ monthLabel }}</span>
              <Button variant="outline" size="sm" @click="goMonth(1)">{{ t('admin.calendar.nextMonth') }}</Button>
            </div>
            <div class="flex items-end gap-2">
              <div class="flex flex-col gap-1.5">
                <Label for="cal-year">{{ t('admin.calendar.yearLabel') }}</Label>
                <select
                  id="cal-year" v-model.number="selectedYear"
                  class="h-9 rounded-md border border-input bg-transparent px-3 text-sm"
                >
                  <option v-for="y in yearOptions" :key="y" :value="y">{{ y }}</option>
                </select>
              </div>
              <div class="flex flex-col gap-1.5">
                <Label for="cal-month">{{ t('admin.calendar.monthLabel') }}</Label>
                <select
                  id="cal-month" v-model.number="selectedMonth"
                  class="h-9 rounded-md border border-input bg-transparent px-3 text-sm"
                >
                  <option v-for="m in 12" :key="m" :value="m">{{ m }}</option>
                </select>
              </div>
              <Button
                type="button" variant="outline" size="icon" :disabled="pending || dayPending"
                :aria-label="t('admin.calendar.refresh')" :title="t('admin.calendar.refresh')"
                @click="refresh(); refreshDay()"
              >
                <RefreshCwIcon class="size-4" :class="{ 'animate-spin': pending || dayPending }" />
              </Button>
            </div>
          </div>

          <div class="grid grid-cols-7 gap-px overflow-hidden rounded-md border border-border bg-border text-xs">
            <div
              v-for="wd in weekdayLabels" :key="wd"
              class="bg-muted px-2 py-1 text-center font-medium text-muted-foreground"
            >
              {{ wd }}
            </div>
            <button
              v-for="cell in gridCells" :key="cell.dateStr"
              type="button"
              class="flex min-h-20 flex-col items-start gap-1 bg-card p-1.5 text-left hover:bg-accent"
              :class="[
                cell.inMonth ? 'text-foreground' : 'text-muted-foreground/50',
                cell.dateStr === selectedDate ? 'ring-2 ring-inset ring-primary' : '',
              ]"
              @click="selectedDate = cell.dateStr"
            >
              <span class="text-xs">{{ cell.day }}</span>
              <span v-if="cell.count" class="rounded bg-primary/15 px-1 text-[11px] font-medium text-primary">
                {{ cell.count }}
              </span>
            </button>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardDescription>{{ t('admin.calendar.dayListTitle', { date: selectedDate }) }}</CardDescription>
        </CardHeader>
        <CardContent class="max-h-[70vh] space-y-2 overflow-y-auto">
          <p v-if="dayPending" class="text-sm text-muted-foreground">{{ t('common.loading') }}</p>
          <p v-else-if="!dayItems.length" class="text-sm text-muted-foreground">{{ t('admin.calendar.empty') }}</p>
          <button
            v-for="item in dayItems" :key="item.id"
            type="button"
            class="flex w-full flex-col gap-0.5 rounded-md border border-border p-2 text-left text-sm hover:bg-accent"
            @click="navigateTo(`/admin/reservations/${item.id}`)"
          >
            <span class="flex items-center justify-between gap-2">
              <span class="font-medium text-foreground">{{ item.name }}</span>
              <span
                class="shrink-0 rounded px-1.5 py-0.5 text-[11px] font-medium"
                :class="item.status === 'Visited' ? 'bg-secondary/25 text-secondary-foreground' : 'bg-primary/15 text-primary'"
              >
                {{ t(`status.${item.status}`) }}
              </span>
            </span>
            <span class="text-xs text-muted-foreground">
              {{ t('admin.calendar.colTime') }} {{ item.visitTime?.slice(0, 5) ?? '-' }}
              · {{ item.consultantName ?? t('admin.reservations.unassigned') }}
            </span>
          </button>
        </CardContent>
      </Card>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { ReservationCalendarDayCount, ReservationCalendarItem } from '~/types/reservation'
import { RefreshCwIcon } from '@lucide/vue'
import { getKstToday } from '~/utils/datetime'

definePageMeta({ middleware: 'admin', layout: 'admin', i18n: false })
useHead({ title: '예약 달력 | Admin', meta: [{ name: 'robots', content: 'noindex, nofollow' }] })

const { t, locale } = useI18n()
const route = useRoute()

function pad(n: number) {
  return String(n).padStart(2, '0')
}

const today = getKstToday()

// 월 이동 시 year·month 쿼리로만 재조회한다(12-6절) — 서버는 이 달의 표시 그리드 전체(최대 6주=42일,
// 이전달 말주·다음달 초주 포함)를 반환한다. 연/월 드롭다운도 이 쿼리를 바꾸는 것뿐이라 별도 API 불필요.
const query = computed(() => ({
  year: Number(route.query.year) || today.year,
  month: Number(route.query.month) || today.month,
}))

// 드롭다운 선택 범위 — 이 시스템 운영 연도(today.year) 기준 앞뒤 2년. 더 넓은 범위가 필요하면 조정.
const yearOptions = computed(() => Array.from({ length: 5 }, (_, i) => today.year - 2 + i))

const selectedYear = computed({
  get: () => query.value.year,
  set: (y: number) => navigateTo({ query: { year: y, month: query.value.month } }),
})
const selectedMonth = computed({
  get: () => query.value.month,
  set: (m: number) => navigateTo({ query: { year: query.value.year, month: m } }),
})

// 🔴 성능(2026-08-27, "날짜 클릭 전인데 왜 다 로드돼있냐" 사용자 지적) — 이전엔 이 API가 42일치
// 예약 상세를 전부 반환해, 날짜를 클릭하기도 전에 한 달치 데이터가 통째로 로드돼 있었다. 그리드가
// 실제로 쓰는 건 날짜별 배지 숫자뿐이라 건수만 받는다(2단계 구조 ① — 그리드 배지용).
const { data: counts, pending, refresh } = await useApi<ReservationCalendarDayCount[]>('/api/admin/reservations/calendar', { query })

const selectedDate = ref(
  query.value.year === today.year && query.value.month === today.month
    ? `${today.year}-${pad(today.month)}-${pad(today.day)}`
    : `${query.value.year}-${pad(query.value.month)}-01`,
)

// 2단계 구조 ② — 우측 "선택한 날짜의 예약 목록"은 selectedDate가 바뀔 때만(최초 진입·날짜 클릭·월
// 이동) 그 하루치를 불러온다. useApi가 query 변화를 반응형으로 추적해 자동 재조회하므로 클릭
// 핸들러에서 직접 fetch를 호출할 필요가 없다(admin/index.vue의 includeInactive 반응형 조회와 동일 패턴).
// 최초 진입 시점엔 <script setup> 최상위 await라 SSR로 함께 프리로드되어(화면 깜빡임 금지 원칙 충족),
// 이후 날짜를 바꿀 때만 클라이언트에서 그 시점부터 로딩 상태(dayPending)를 보여주며 재조회한다.
const dayQuery = computed(() => ({ date: selectedDate.value }))
const { data: dayItemsData, pending: dayPending, refresh: refreshDay } =
  await useApi<ReservationCalendarItem[]>('/api/admin/reservations/calendar/day', { query: dayQuery })
const dayItems = computed(() => dayItemsData.value ?? [])

// 예약 확정 시 조용히 새로고침(2026-08-27) — admin.vue 레이아웃이 연결한 SSE를 여기서 구독만 한다.
// 이 페이지가 마운트돼 있는 동안만 watch가 살아있어 "달력을 보고 있는 계정만" 반응하는 게 자동으로 됨.
// 그리드 배지(counts)·선택일 목록(dayItems) 둘 다 갱신해야 한다.
const reservationConfirmedTick = useState('sse:reservationConfirmedTick', () => 0)
watch(reservationConfirmedTick, () => { refresh(); refreshDay() })

// 월 이동 시 이전 달의 "선택한 날짜"를 그대로 들고 있으면 새 달과 안 맞으므로 1일로 리셋한다.
watch(query, () => {
  selectedDate.value = `${query.value.year}-${pad(query.value.month)}-01`
})

// SSR·클라이언트 호스트의 로컬 타임존과 무관하게 항상 같은 그리드가 나오도록 UTC 기준으로만 계산한다.
const monthLabel = computed(() =>
  new Intl.DateTimeFormat(locale.value, { year: 'numeric', month: 'long', timeZone: 'UTC' })
    .format(new Date(Date.UTC(query.value.year, query.value.month - 1, 1))))

const weekdayLabels = computed(() => {
  const fmt = new Intl.DateTimeFormat(locale.value, { weekday: 'short', timeZone: 'UTC' })
  // 2024-01-07(UTC)은 일요일 — 요일 라벨만 뽑기 위한 기준 주
  return Array.from({ length: 7 }, (_, i) => fmt.format(new Date(Date.UTC(2024, 0, 7 + i))))
})

const countByDate = computed(() => {
  const map = new Map<string, number>()
  for (const c of counts.value ?? []) map.set(c.visitDate, c.count)
  return map
})

const gridCells = computed(() => {
  const y = query.value.year
  const m = query.value.month
  const startWeekday = new Date(Date.UTC(y, m - 1, 1)).getUTCDay() // 0=일요일
  const gridStartMs = Date.UTC(y, m - 1, 1 - startWeekday)

  return Array.from({ length: 42 }, (_, i) => {
    const d = new Date(gridStartMs + i * 86_400_000)
    const dateStr = `${d.getUTCFullYear()}-${pad(d.getUTCMonth() + 1)}-${pad(d.getUTCDate())}`
    return {
      dateStr,
      day: d.getUTCDate(),
      inMonth: d.getUTCMonth() === m - 1,
      count: countByDate.value.get(dateStr) ?? 0,
    }
  })
})

function goMonth(delta: number) {
  let y = query.value.year
  let m = query.value.month + delta
  if (m < 1) { m = 12; y -= 1 }
  else if (m > 12) { m = 1; y += 1 }
  navigateTo({ query: { year: y, month: m } })
}
</script>
