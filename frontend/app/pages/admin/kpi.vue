<template>
  <div class="space-y-6">
    <h1 class="text-xl font-semibold text-foreground">{{ t('admin.kpi.title') }}</h1>

    <Card>
      <CardContent class="flex flex-wrap items-end gap-4">
        <div class="flex flex-col gap-1.5">
          <Label for="f-from">{{ t('admin.kpi.filterFrom') }}</Label>
          <DatePicker id="f-from" v-model="formFrom" :locale="inputLang" />
        </div>
        <div class="flex flex-col gap-1.5">
          <Label for="f-to">{{ t('admin.kpi.filterTo') }}</Label>
          <DatePicker id="f-to" v-model="formTo" :locale="inputLang" :min-value="toMinValue" />
        </div>
        <Button :disabled="rangeTooLong" @click="applyFilters">{{ t('admin.kpi.filterApply') }}</Button>
        <p v-if="rangeTooLong" class="w-full text-sm text-destructive">{{ t('admin.common.filterRangeError') }}</p>
        <p v-else-if="dateRangeError" class="w-full text-sm text-destructive">{{ dateRangeError }}</p>
      </CardContent>
    </Card>

    <Card>
      <CardContent>
        <ClientOnly>
          <div style="height: 320px">
            <Bar :data="chartData" :options="chartOptions" />
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
            <th class="px-3 py-2 text-left">{{ t('admin.kpi.colConsultant') }}</th>
            <th class="px-3 py-2 text-right">{{ t('admin.kpi.colAssigned') }}</th>
            <th class="px-3 py-2 text-right">{{ t('admin.kpi.colConfirmed') }}</th>
            <th class="px-3 py-2 text-right">{{ t('admin.kpi.colVisited') }}</th>
            <th class="px-3 py-2 text-right">{{ t('admin.kpi.colConversionRate') }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="!data?.length">
            <td colspan="5" class="p-6 text-center text-muted-foreground">{{ t('admin.kpi.empty') }}</td>
          </tr>
          <tr v-for="r in data" :key="r.consultantId" class="border-t border-border">
            <td class="px-3 py-2">{{ r.consultantName }}</td>
            <td class="px-3 py-2 text-right">{{ r.assigned }}</td>
            <td class="px-3 py-2 text-right">{{ r.confirmed }}</td>
            <td class="px-3 py-2 text-right">{{ r.visited }}</td>
            <td class="px-3 py-2 text-right">{{ r.conversionRate }}%</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<script setup lang="ts">
import { Bar } from 'vue-chartjs'
import type { ConsultantKpi } from '~/types/reservation'
import { clampDateRangeEnd, todayKst } from '~/utils/datetime'

definePageMeta({ middleware: 'admin', layout: 'admin', i18n: false })
useHead({ title: '실장 KPI | Admin', meta: [{ name: 'robots', content: 'noindex, nofollow' }] })

const { t } = useI18n()
const route = useRoute()
// layouts/admin.vue가 useOpsLocale()을 이미 호출해 locale이 계정 값으로 맞춰져 있다 — 여기선 재사용만.
const inputLang = useInputLang()

const defaultFrom = `${todayKst().slice(0, 7)}-01`
const defaultTo = todayKst()

// 🔴 검색 입력을 반응형 query에 직접 물리지 말 것(12-4절)과 동일 이유로 URL 쿼리를 computed로 감싼다.
// 🔴 조회 기간 상한(1년+1일)은 useDateRangeFilter가 필터 폼(UI)만 막는다 — URL 직접 조작·북마크는
// 폼을 거치지 않아 그 방어를 우회한다. 실제 조회에 쓰는 이 query 자체에서 clamp해 우회를 막는다.
const query = computed(() => {
  const from = (route.query.from as string) || defaultFrom
  return { from, to: clampDateRangeEnd(from, (route.query.to as string) || defaultTo) }
})

const { data, error } = await useApi<ConsultantKpi[]>('/api/admin/stats/consultants', { query })

const formFrom = ref(query.value.from)
const formTo = ref(query.value.to)
const { toMinValue, rangeTooLong } = useDateRangeFilter(formFrom, formTo)
// 🔴 2026-09-01 감사 — 서버가 {code:"INVALID_DATE_RANGE"}로 거부해도(예: URL 직접 조작으로
// to<from 전달) 화면엔 아무 안내 없이 그냥 빈 표로만 보였다. rangeTooLong(폼 단계 사전 차단)과
// 별개로, 실제로 보낸 요청이 서버에서 거부된 경우를 위 errors.* i18n 네임스페이스로 표시한다.
const dateRangeError = computed(() => {
  const code = (error.value as any)?.data?.code
  return code ? t(`errors.${code}`) : null
})

function applyFilters() {
  if (rangeTooLong.value) return
  navigateTo({ query: { from: formFrom.value, to: formTo.value } })
}

// D20 Olive Garden Feast 팔레트를 재사용한다(D21) — 차트 전용 색상을 새로 만들지 않는다.
const COLOR_PRIMARY = '#606C38'
const COLOR_FOREGROUND = '#283618'
const COLOR_SECONDARY = '#DDA15E'

const chartData = computed(() => ({
  labels: (data.value ?? []).map(r => r.consultantName),
  datasets: [
    { label: t('admin.kpi.colAssigned'), data: (data.value ?? []).map(r => r.assigned), backgroundColor: COLOR_FOREGROUND },
    { label: t('admin.kpi.colConfirmed'), data: (data.value ?? []).map(r => r.confirmed), backgroundColor: COLOR_PRIMARY },
    { label: t('admin.kpi.colVisited'), data: (data.value ?? []).map(r => r.visited), backgroundColor: COLOR_SECONDARY },
  ],
}))
const chartOptions = { responsive: true, maintainAspectRatio: false }
</script>
