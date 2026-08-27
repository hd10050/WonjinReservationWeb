<template>
  <div class="space-y-6">
    <h1 class="text-xl font-semibold text-foreground">{{ t('admin.stats.title') }}</h1>

    <Card>
      <CardContent class="flex flex-wrap items-end gap-4 pt-6">
        <div class="flex flex-col gap-1.5">
          <Label for="f-from">{{ t('admin.stats.filterFrom') }}</Label>
          <DatePicker id="f-from" v-model="formFrom" :locale="inputLang" class="w-40" />
        </div>
        <div class="flex flex-col gap-1.5">
          <Label for="f-to">{{ t('admin.stats.filterTo') }}</Label>
          <DatePicker id="f-to" v-model="formTo" :locale="inputLang" class="w-40" />
        </div>
        <Button @click="applyFilters">{{ t('admin.stats.filterApply') }}</Button>
      </CardContent>
    </Card>

    <section class="space-y-3">
      <h2 class="text-lg font-medium text-foreground">{{ t('admin.stats.sectionWeekly') }}</h2>
      <Card>
        <CardContent class="pt-6">
          <ClientOnly>
            <div style="height: 320px">
              <Line :data="weeklyChartData" :options="chartOptions" />
            </div>
            <template #fallback>
              <div style="height: 320px" />
            </template>
          </ClientOnly>
        </CardContent>
      </Card>
      <div class="overflow-x-auto rounded-md border border-border">
        <table class="w-full text-sm">
          <thead class="bg-muted text-muted-foreground">
            <tr>
              <th class="px-3 py-2 text-left">{{ t('admin.stats.colWeekStart') }}</th>
              <th class="px-3 py-2 text-right">{{ t('admin.stats.colReceived') }}</th>
              <th class="px-3 py-2 text-right">{{ t('admin.stats.colConfirmed') }}</th>
              <th class="px-3 py-2 text-right">{{ t('admin.stats.colVisited') }}</th>
              <th class="px-3 py-2 text-right">{{ t('admin.stats.colCancelled') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="!data?.weekly.length">
              <td colspan="5" class="p-6 text-center text-muted-foreground">{{ t('admin.stats.empty') }}</td>
            </tr>
            <tr v-for="w in data?.weekly" :key="w.weekStart" class="border-t border-border">
              <td class="px-3 py-2">{{ w.weekStart }}</td>
              <td class="px-3 py-2 text-right">{{ w.received }}</td>
              <td class="px-3 py-2 text-right">{{ w.confirmed }}</td>
              <td class="px-3 py-2 text-right">{{ w.visited }}</td>
              <td class="px-3 py-2 text-right">{{ w.cancelled }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>

    <section class="space-y-3">
      <h2 class="text-lg font-medium text-foreground">{{ t('admin.stats.sectionProcedures') }}</h2>
      <Card>
        <CardContent class="pt-6">
          <ClientOnly>
            <div style="height: 320px">
              <Bar :data="procedureChartData" :options="chartOptions" />
            </div>
            <template #fallback>
              <div style="height: 320px" />
            </template>
          </ClientOnly>
        </CardContent>
      </Card>
      <div class="overflow-x-auto rounded-md border border-border">
        <table class="w-full text-sm">
          <thead class="bg-muted text-muted-foreground">
            <tr>
              <th class="px-3 py-2 text-left">{{ t('admin.stats.colProcedure') }}</th>
              <th class="px-3 py-2 text-right">{{ t('admin.stats.colCount') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="!data?.procedures.length">
              <td colspan="2" class="p-6 text-center text-muted-foreground">{{ t('admin.stats.empty') }}</td>
            </tr>
            <tr v-for="p in data?.procedures" :key="p.procedureId" class="border-t border-border">
              <td class="px-3 py-2">{{ procedureName(p) }}</td>
              <td class="px-3 py-2 text-right">{{ p.count }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>

    <section class="space-y-3">
      <h2 class="text-lg font-medium text-foreground">{{ t('admin.stats.sectionLocales') }}</h2>
      <Card>
        <CardContent class="pt-6">
          <ClientOnly>
            <div style="height: 320px">
              <Doughnut :data="localeChartData" :options="chartOptions" />
            </div>
            <template #fallback>
              <div style="height: 320px" />
            </template>
          </ClientOnly>
        </CardContent>
      </Card>
      <div class="overflow-x-auto rounded-md border border-border">
        <table class="w-full text-sm">
          <thead class="bg-muted text-muted-foreground">
            <tr>
              <th class="px-3 py-2 text-left">{{ t('admin.stats.colLocale') }}</th>
              <th class="px-3 py-2 text-right">{{ t('admin.stats.colCount') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="!data?.locales.length">
              <td colspan="2" class="p-6 text-center text-muted-foreground">{{ t('admin.stats.empty') }}</td>
            </tr>
            <tr v-for="l in data?.locales" :key="l.locale" class="border-t border-border">
              <td class="px-3 py-2">{{ l.locale }}</td>
              <td class="px-3 py-2 text-right">{{ l.count }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>

    <section class="space-y-3">
      <h2 class="text-lg font-medium text-foreground">{{ t('admin.stats.sectionConsultants') }}</h2>
      <Card>
        <CardContent class="pt-6">
          <ClientOnly>
            <div style="height: 320px">
              <Bar :data="consultantChartData" :options="chartOptions" />
            </div>
            <template #fallback>
              <div style="height: 320px" />
            </template>
          </ClientOnly>
        </CardContent>
      </Card>
      <div class="overflow-x-auto rounded-md border border-border">
        <table class="w-full text-sm">
          <thead class="bg-muted text-muted-foreground">
            <tr>
              <th class="px-3 py-2 text-left">{{ t('admin.stats.colConsultant') }}</th>
              <th class="px-3 py-2 text-right">{{ t('admin.stats.colCount') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="!data?.consultants.length">
              <td colspan="2" class="p-6 text-center text-muted-foreground">{{ t('admin.stats.empty') }}</td>
            </tr>
            <tr v-for="c in data?.consultants" :key="c.consultantId" class="border-t border-border">
              <td class="px-3 py-2">{{ c.consultantName }}</td>
              <td class="px-3 py-2 text-right">{{ c.count }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
import { Bar, Line, Doughnut } from 'vue-chartjs'
import type { ProcedureStat, ReservationStats } from '~/types/reservation'
import { todayKst } from '~/utils/datetime'

definePageMeta({ middleware: 'admin', layout: 'admin', i18n: false })
useHead({ title: '예약 통계 | Admin', meta: [{ name: 'robots', content: 'noindex, nofollow' }] })

const { t, locale } = useI18n()
const route = useRoute()
// layouts/admin.vue가 useOpsLocale()을 이미 호출해 locale이 계정 값으로 맞춰져 있다 — 여기선 재사용만.
const inputLang = useInputLang()

const defaultFrom = `${todayKst().slice(0, 7)}-01`
const defaultTo = todayKst()

// 🔴 검색 입력을 반응형 query에 직접 물리지 말 것(12-4절)과 동일 이유로 URL 쿼리를 computed로 감싼다.
const query = computed(() => ({
  from: (route.query.from as string) || defaultFrom,
  to: (route.query.to as string) || defaultTo,
}))

const { data } = await useApi<ReservationStats>('/api/admin/stats/reservations', { query })

const formFrom = ref(query.value.from)
const formTo = ref(query.value.to)

function applyFilters() {
  navigateTo({ query: { from: formFrom.value, to: formTo.value } })
}

// 관리자 UI 언어(useOpsLocale)에 맞는 시술명 1개만 표시, 미매칭 시 한국어 폴백
function procedureName(p: ProcedureStat): string {
  const map: Record<string, string> = { 'zh-CN': p.nameZhCn, 'zh-TW': p.nameZhTw, en: p.nameEn, ko: p.nameKo }
  return map[locale.value] ?? p.nameKo
}

// D20 Olive Garden Feast 팔레트를 재사용한다(D21) — 차트 전용 색상을 새로 만들지 않는다.
const COLOR_PRIMARY = '#606C38'
const COLOR_FOREGROUND = '#283618'
const COLOR_SECONDARY = '#DDA15E'
const COLOR_DESTRUCTIVE = '#BC6C25'
const PALETTE = [COLOR_PRIMARY, COLOR_SECONDARY, COLOR_FOREGROUND, COLOR_DESTRUCTIVE]

const chartOptions = { responsive: true, maintainAspectRatio: false }

const weeklyChartData = computed(() => ({
  labels: (data.value?.weekly ?? []).map(w => w.weekStart),
  datasets: [
    { label: t('admin.stats.colReceived'), data: (data.value?.weekly ?? []).map(w => w.received), borderColor: COLOR_FOREGROUND, backgroundColor: COLOR_FOREGROUND },
    { label: t('admin.stats.colConfirmed'), data: (data.value?.weekly ?? []).map(w => w.confirmed), borderColor: COLOR_PRIMARY, backgroundColor: COLOR_PRIMARY },
    { label: t('admin.stats.colVisited'), data: (data.value?.weekly ?? []).map(w => w.visited), borderColor: COLOR_SECONDARY, backgroundColor: COLOR_SECONDARY },
    { label: t('admin.stats.colCancelled'), data: (data.value?.weekly ?? []).map(w => w.cancelled), borderColor: COLOR_DESTRUCTIVE, backgroundColor: COLOR_DESTRUCTIVE },
  ],
}))

const procedureChartData = computed(() => ({
  labels: (data.value?.procedures ?? []).map(p => procedureName(p)),
  datasets: [{ label: t('admin.stats.colCount'), data: (data.value?.procedures ?? []).map(p => p.count), backgroundColor: COLOR_PRIMARY }],
}))

const localeChartData = computed(() => ({
  labels: (data.value?.locales ?? []).map(l => l.locale),
  datasets: [{ data: (data.value?.locales ?? []).map(l => l.count), backgroundColor: PALETTE }],
}))

const consultantChartData = computed(() => ({
  labels: (data.value?.consultants ?? []).map(c => c.consultantName),
  datasets: [{ label: t('admin.stats.colCount'), data: (data.value?.consultants ?? []).map(c => c.count), backgroundColor: COLOR_PRIMARY }],
}))
</script>
