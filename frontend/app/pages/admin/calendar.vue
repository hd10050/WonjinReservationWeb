<template>
  <div class="space-y-6">
    <h1 class="text-xl font-semibold text-foreground">{{ t('admin.calendar.title') }}</h1>

    <div class="grid grid-cols-1 gap-6 lg:grid-cols-[1fr_320px]">
      <Card>
        <CardContent class="pt-6">
          <div class="mb-4 flex items-center justify-between">
            <Button variant="outline" size="sm" @click="goMonth(-1)">{{ t('admin.calendar.prevMonth') }}</Button>
            <span class="font-semibold text-foreground">{{ monthLabel }}</span>
            <Button variant="outline" size="sm" @click="goMonth(1)">{{ t('admin.calendar.nextMonth') }}</Button>
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
              <span v-if="cell.items.length" class="rounded bg-primary/15 px-1 text-[11px] font-medium text-primary">
                {{ cell.items.length }}
              </span>
            </button>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardDescription>{{ t('admin.calendar.dayListTitle', { date: selectedDate }) }}</CardDescription>
        </CardHeader>
        <CardContent class="space-y-2">
          <p v-if="!selectedItems.length" class="text-sm text-muted-foreground">{{ t('admin.calendar.empty') }}</p>
          <button
            v-for="item in selectedItems" :key="item.id"
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
import type { ReservationCalendarItem } from '~/types/reservation'
import { getKstToday } from '~/utils/datetime'

definePageMeta({ middleware: 'admin', layout: 'admin', i18n: false })
useHead({ title: '예약 달력 | Admin', meta: [{ name: 'robots', content: 'noindex, nofollow' }] })

const { t, locale } = useI18n()
const route = useRoute()

function pad(n: number) {
  return String(n).padStart(2, '0')
}

const today = getKstToday()

// 월 이동 시 year·month 쿼리로만 재조회한다(12-6절) — 서버는 정확히 이 한 달만 반환한다.
const query = computed(() => ({
  year: Number(route.query.year) || today.year,
  month: Number(route.query.month) || today.month,
}))

const { data, refresh } = await useApi<ReservationCalendarItem[]>('/api/admin/reservations/calendar', { query })

// 예약 확정 시 조용히 새로고침(2026-08-27) — admin.vue 레이아웃이 연결한 SSE를 여기서 구독만 한다.
// 이 페이지가 마운트돼 있는 동안만 watch가 살아있어 "달력을 보고 있는 계정만" 반응하는 게 자동으로 됨.
const reservationConfirmedTick = useState('sse:reservationConfirmedTick', () => 0)
watch(reservationConfirmedTick, () => { refresh() })

const selectedDate = ref(
  query.value.year === today.year && query.value.month === today.month
    ? `${today.year}-${pad(today.month)}-${pad(today.day)}`
    : `${query.value.year}-${pad(query.value.month)}-01`,
)

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

const itemsByDate = computed(() => {
  const map = new Map<string, ReservationCalendarItem[]>()
  for (const item of data.value ?? []) {
    const list = map.get(item.visitDate) ?? []
    list.push(item)
    map.set(item.visitDate, list)
  }
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
      items: itemsByDate.value.get(dateStr) ?? [],
    }
  })
})

const selectedItems = computed(() => itemsByDate.value.get(selectedDate.value) ?? [])

function goMonth(delta: number) {
  let y = query.value.year
  let m = query.value.month + delta
  if (m < 1) { m = 12; y -= 1 }
  else if (m > 12) { m = 1; y += 1 }
  navigateTo({ query: { year: y, month: m } })
}
</script>
